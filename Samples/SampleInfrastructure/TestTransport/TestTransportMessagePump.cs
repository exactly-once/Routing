﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace SampleInfrastructure.TestTransport
{
    class TestTransportMessagePump : IPushMessages
    {
        public TestTransportMessagePump(string basePath)
        {
            this.basePath = basePath;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            this.criticalError = criticalError;

            transactionMode = settings.RequiredTransactionMode;

            PathChecker.ThrowForBadPath(settings.InputQueue, "InputQueue");

            messagePumpBasePath = Path.Combine(basePath, settings.InputQueue);
            bodyDir = Path.Combine(messagePumpBasePath, BodyDirName);
            delayedDir = Path.Combine(messagePumpBasePath, DelayedDirName);

            pendingTransactionDir = Path.Combine(messagePumpBasePath, PendingDirName);
            committedTransactionDir = Path.Combine(messagePumpBasePath, CommittedDirName);

            if (settings.PurgeOnStartup)
            {
                if (Directory.Exists(messagePumpBasePath))
                {
                    Directory.Delete(messagePumpBasePath, true);
                }
            }

            delayedMessagePoller = new DelayedMessagePoller(messagePumpBasePath, delayedDir);

            return Task.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            maxConcurrency = limitations.MaxConcurrency;
            concurrencyLimiter = new SemaphoreSlim(maxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;

            RecoverPendingTransactions();

            EnsureDirectoriesExists();

            messagePumpTask = Task.Run(ProcessMessages, cancellationToken);

            delayedMessagePoller.Start();
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();

            await delayedMessagePoller.Stop()
                .ConfigureAwait(false);

            await messagePumpTask
                .ConfigureAwait(false);

            while (concurrencyLimiter.CurrentCount != maxConcurrency)
            {
                await Task.Delay(50, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            concurrencyLimiter.Dispose();
        }

        void RecoverPendingTransactions()
        {
            if (transactionMode != TransportTransactionMode.None)
            {
                DirectoryBasedTransaction.RecoverPartiallyCompletedTransactions(messagePumpBasePath, PendingDirName, CommittedDirName);
            }
            else
            {
                if (Directory.Exists(pendingTransactionDir))
                {
                    Directory.Delete(pendingTransactionDir, true);
                }
            }
        }

        void EnsureDirectoriesExists()
        {
            Directory.CreateDirectory(messagePumpBasePath);
            Directory.CreateDirectory(bodyDir);
            Directory.CreateDirectory(delayedDir);
            Directory.CreateDirectory(pendingTransactionDir);

            if (transactionMode != TransportTransactionMode.None)
            {
                Directory.CreateDirectory(committedTransactionDir);
            }
        }

        [DebuggerNonUserCode]
        async Task ProcessMessages()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await InnerProcessMessages()
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failure to process messages", ex);
                }
            }
        }

        async Task InnerProcessMessages()
        {
            log.Debug($"Started polling for new messages in {messagePumpBasePath}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var filesFound = false;

                foreach (var filePath in Directory.EnumerateFiles(messagePumpBasePath, "*.*"))
                {
                    filesFound = true;

                    var nativeMessageId = Path.GetFileNameWithoutExtension(filePath).Replace(".metadata", "");

                    await concurrencyLimiter.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    ITestTransportTransaction transaction;

                    try
                    {
                        transaction = GetTransaction();

                        var ableToLockFile = await transaction.BeginTransaction(filePath).ConfigureAwait(false);

                        if (!ableToLockFile)
                        {
                            log.Debug($"Unable to lock file {filePath}({transaction.FileToProcess})");
                            concurrencyLimiter.Release();
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Debug($"Failed to begin transaction {filePath}", ex);

                        concurrencyLimiter.Release();
                        throw;
                    }

                    ProcessFile(transaction, nativeMessageId)
                        .ContinueWith(t =>
                        {
                            try
                            {
                                if (log.IsDebugEnabled)
                                {
                                    log.Debug($"Completing processing for {filePath}({transaction.FileToProcess}), exception (if any): {t.Exception}");
                                }

                                var wasCommitted = transaction.Complete();

                                if (wasCommitted)
                                {
                                    File.Delete(Path.Combine(bodyDir, nativeMessageId + BodyFileSuffix));
                                }

                                if (t.Exception != null)
                                {
                                    var baseEx = t.Exception.GetBaseException();

                                    if (!(baseEx is OperationCanceledException))
                                    {
                                        criticalError.Raise($"Failed to process {filePath}({transaction.FileToProcess})", baseEx);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                criticalError.Raise($"Failure while trying to complete receive transaction for  {filePath}({transaction.FileToProcess})" + filePath, ex);
                            }
                            finally
                            {
                                concurrencyLimiter.Release();
                            }
                        }, CancellationToken.None)
                        .Ignore();
                }

                if (!filesFound)
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        ITestTransportTransaction GetTransaction()
        {
            if (transactionMode == TransportTransactionMode.None)
            {
                return new NoTransaction(messagePumpBasePath, PendingDirName);
            }

            return new DirectoryBasedTransaction(messagePumpBasePath, PendingDirName, CommittedDirName, Guid.NewGuid().ToString());
        }

        async Task ProcessFile(ITestTransportTransaction transaction, string messageId)
        {
            var message = await AsyncFile.ReadText(transaction.FileToProcess)
                    .ConfigureAwait(false);

            var bodyPath = Path.Combine(bodyDir, $"{messageId}{BodyFileSuffix}");
            var headers = HeaderSerializer.Deserialize(message);

            if (headers.TryGetValue(TestTransportHeaders.TimeToBeReceived, out var ttbrString))
            {
                headers.Remove(TestTransportHeaders.TimeToBeReceived);

                var ttbr = TimeSpan.Parse(ttbrString);

                //file.move preserves create time
                var sentTime = File.GetCreationTimeUtc(transaction.FileToProcess);

                var utcNow = DateTime.UtcNow;
                if (sentTime + ttbr < utcNow)
                {
                    await transaction.Commit()
                        .ConfigureAwait(false);
                    log.InfoFormat("Dropping message '{0}' as the specified TimeToBeReceived of '{1}' expired since sending the message at '{2:O}'. Current UTC time is '{3:O}'", messageId, ttbrString, sentTime, utcNow);
                    return;
                }
            }

            var tokenSource = new CancellationTokenSource();

            var body = await AsyncFile.ReadBytes(bodyPath, cancellationToken)
                .ConfigureAwait(false);

            var transportTransaction = new TransportTransaction();

            if (transactionMode == TransportTransactionMode.SendsAtomicWithReceive)
            {
                transportTransaction.Set(transaction);
            }

            var messageContext = new MessageContext(messageId, headers, body, transportTransaction, tokenSource, new ContextBag());

            try
            {
                await onMessage(messageContext)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                transaction.ClearPendingOutgoingOperations();
                var processingFailures = retryCounts.AddOrUpdate(messageId, id => 1, (id, currentCount) => currentCount + 1);

                var errorContext = new ErrorContext(exception, headers, messageId, body, transportTransaction, processingFailures);

                // the transport tests assume that all transports use a circuit breaker to be resilient against exceptions
                // in onError. Since we don't need that robustness, we just retry onError once should it fail.
                ErrorHandleResult actionToTake;
                try
                {
                    actionToTake = await onError(errorContext)
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    actionToTake = await onError(errorContext)
                        .ConfigureAwait(false);
                }

                if (actionToTake == ErrorHandleResult.RetryRequired)
                {
                    transaction.Rollback();

                    return;
                }
            }

            if (tokenSource.IsCancellationRequested)
            {
                transaction.Rollback();

                return;
            }

            await transaction.Commit()
                .ConfigureAwait(false);
        }

        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;
        Task messagePumpTask;
        Func<MessageContext, Task> onMessage;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
        ConcurrentDictionary<string, int> retryCounts = new ConcurrentDictionary<string, int>();
        string messagePumpBasePath;
        string basePath;
        DelayedMessagePoller delayedMessagePoller;
        TransportTransactionMode transactionMode;
        int maxConcurrency;
        string bodyDir;
        string pendingTransactionDir;
        string committedTransactionDir;
        string delayedDir;

        CriticalError criticalError;


        static ILog log = LogManager.GetLogger<TestTransportMessagePump>();

        public const string BodyFileSuffix = ".body.txt";
        public const string BodyDirName = ".bodies";
        public const string DelayedDirName = ".delayed";

        const string CommittedDirName = ".committed";
        const string PendingDirName = ".pending";
    }
}

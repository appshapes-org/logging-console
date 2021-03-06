﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using Xunit;

namespace AppShapes.Logging.Console.Tests
{
    public class ConsoleLoggerProcessorTests
    {
        [Fact]
        public void ConstructorMustThrowExceptionWhenConsoleLoggerWriterIsNull()
        {
            Assert.Equal("console cannot be null (Parameter 'console')", Assert.Throws<ArgumentNullException>(() => new ConsoleLoggerProcessor(null)).Message);
        }

        [Fact]
        public void DisableQueueMustHandleExceptionWhenAnyExceptionOccurs()
        {
            using StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor {Exception = new TerminateQueuingException()};
            processor.InvokeDisableQueue();
            processor.Exception = null;
        }

        [Fact]
        public void DisposeMustDispose()
        {
            StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor();
            processor.Dispose();
            Assert.True(processor.IsQueueDisabled);
            Assert.Equal(ThreadState.Stopped, processor.GetThread().ThreadState);
        }

        [Fact]
        public void DisposeMustHandleThreadStateException()
        {
            StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor {Exception = new ThreadStateException()};
            processor.Dispose();
            Assert.True(processor.HandleThreadStateExceptionCalled == 1);
        }

        [Fact]
        public void EnqueueMustHandleInvalidOperationExceptionWhenAddToQueueThrowsInvalidOperationException()
        {
            using StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor {Exception = new InvalidOperationException()};
            Assert.True(processor.GetQueue().Count == 0);
            processor.InvokeEnqueue("Test");
            Assert.True(processor.GetQueue().Count == 0);
        }

        [Fact]
        public void GetProcessorThreadMustCreateBackgroundThreadWhenCalled()
        {
            using StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor();
            processor.Process("Test");
            Assert.True(processor.GetThread().IsBackground);
            Assert.Equal($"{nameof(ConsoleLoggerProcessor)}", processor.GetThread().Name);
        }

        [Fact]
        public void ProcessMessagesMustDisableQueueWhenExceptionIsCaught()
        {
            using StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor {Exception = new WriteMessagesException()};
            Assert.False(processor.IsQueueDisabled);
            processor.InvokeProcessMessages();
            Assert.True(processor.IsQueueDisabled);
        }

        [Fact]
        public void ProcessMustNotQueueMessageWhenQueueIsNotEnabled()
        {
            using StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor();
            processor.InvokeDisableQueue();
            processor.Process("Test");
            Assert.True(processor.EnqueueCalled == 0);
            Assert.True(processor.WriteMessageCalled == 1);
        }

        [Fact]
        public void ProcessMustQueueMessageWhenQueueIsEnabled()
        {
            using StubConsoleLoggerProcessor processor = new StubConsoleLoggerProcessor();
            processor.Process("Test");
            Assert.True(processor.EnqueueCalled == 1);
        }

        private class NullConsole : IConsoleLoggerWriter
        {
            public void Flush()
            {
            }

            public void Write(string message)
            {
            }

            public void WriteLine(string message)
            {
            }
        }

        private class StubConsoleLoggerProcessor : ConsoleLoggerProcessor
        {
            public StubConsoleLoggerProcessor(IConsoleLoggerWriter console = null) : base(console ?? new NullConsole())
            {
            }

            public int EnqueueCalled { get; private set; }

            public Exception Exception { private get; set; }

            public BlockingCollection<string> GetQueue()
            {
                return Queue;
            }

            public Thread GetThread()
            {
                return ProcessorThread;
            }

            public int HandleThreadStateExceptionCalled { get; private set; }

            public void InvokeDisableQueue()
            {
                DisableQueue();
            }

            public void InvokeEnqueue(string message)
            {
                Enqueue(message);
            }

            public void InvokeProcessMessages()
            {
                ProcessMessages();
            }

            public bool IsQueueDisabled => !IsQueueEnabled;

            public int WriteMessageCalled { get; private set; }

            protected override void AddToQueue(string message)
            {
                if (Exception?.GetType() == typeof(InvalidOperationException))
                    throw Exception;
                base.AddToQueue(message);
            }

            protected override void Enqueue(string message)
            {
                base.Enqueue(message);
                ++EnqueueCalled;
            }

            protected override void HandleThreadStateException(ThreadStateException exception)
            {
                ++HandleThreadStateExceptionCalled;
                base.HandleThreadStateException(exception);
            }

            protected override void TerminateProcessing()
            {
                base.TerminateProcessing();
                if (Exception?.GetType() == typeof(ThreadStateException))
                    throw Exception;
            }

            protected override void TerminateQueuing()
            {
                base.TerminateQueuing();
                if (Exception?.GetType() == typeof(TerminateQueuingException))
                    throw Exception;
            }

            protected override void WriteMessage(string message)
            {
                base.WriteMessage(message);
                ++WriteMessageCalled;
            }

            protected override void WriteMessages()
            {
                if (Exception?.GetType() == typeof(WriteMessagesException))
                    throw Exception;
                base.WriteMessages();
            }
        }

        private class TerminateQueuingException : Exception
        {
        }

        private class WriteMessagesException : Exception
        {
        }
    }
}
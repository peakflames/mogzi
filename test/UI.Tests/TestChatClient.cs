using MaxBot;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UI.Tests;

public class TestChatClient : IChatClient
{
    private readonly string _response;

    public TestChatClient(string response = "Default test response")
    {
        _response = response;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var update = new ChatResponseUpdate(ChatRole.Assistant, _response);
        return new[] { update }.ToAsyncEnumerable();
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}

// Helper class to convert a collection to an IAsyncEnumerable for testing.
public static class TestAsyncEnumerable
{
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        return new AsyncEnumerable<T>(source);
    }

    private class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _source;

        public AsyncEnumerable(IEnumerable<T> source)
        {
            _source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator<T>(_source.GetEnumerator(), cancellationToken);
        }
    }

    private class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly CancellationToken _cancellationToken;

        public AsyncEnumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            _enumerator = enumerator;
            _cancellationToken = cancellationToken;
        }

        public T Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<bool>(_enumerator.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return new ValueTask();
        }
    }
}

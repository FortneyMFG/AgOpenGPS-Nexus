using System;
using Aog.Abstractions.Mapping;

namespace Aog.Plugins.Mapping;

internal sealed class FieldContextObserver : IObserver<FieldContext>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(FieldContext value)
    {
    }
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Tomat.SharpLink.Compiler.Collections;

public class MultiKeyDictionary<TKey1, TKey2, TValue> where TKey1 : notnull
                                                      where TKey2 : notnull {
    private readonly Dictionary<TKey1, TValue> dictionary1 = new();
    private readonly Dictionary<TKey2, TValue> dictionary2 = new();

    public TValue this[TKey1 key] {
        get => dictionary1[key];
        set => dictionary1[key] = value;
    }

    public TValue this[TKey2 key] {
        get => dictionary2[key];
        set => dictionary2[key] = value;
    }

    public void Add(TKey1 key1, TKey2 key2, TValue value) {
        dictionary1.Add(key1, value);
        dictionary2.Add(key2, value);
    }

    public bool TryGetValue(TKey1 key, [NotNullWhen(returnValue: true)] out TValue value) {
        return dictionary1.TryGetValue(key, out value!);
    }

    public bool TryGetValue(TKey2 key, [NotNullWhen(returnValue: true)] out TValue value) {
        return dictionary2.TryGetValue(key, out value!);
    }
}

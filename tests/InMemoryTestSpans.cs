using System.Collections;
using System.Diagnostics;

namespace todo_odd.Tests;

public class InMemoryTestSpans : ICollection<Activity>
{
    private Dictionary<string, List<Activity>> _spans = [];
    
    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => false;

    public void Add(Activity item)
    {
        var traceId = item.TraceId.ToString();
        _spans.TryAdd(traceId, []);
        _spans[traceId].Add(item);
    }

    public void Clear()
    {
        _spans.Remove(Activity.Current?.TraceId.ToString());
    }

    public bool Contains(Activity item)
    {
        if (!_spans.TryGetValue(Activity.Current?.TraceId.ToString(), out var activities))
            return false;

        return activities.Contains(item);
    }

    public void CopyTo(Activity[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<Activity> GetEnumerator()
    {
        if (!_spans.TryGetValue(Activity.Current?.TraceId.ToString(), out var activities))
            return Enumerable.Empty<Activity>().GetEnumerator();

        return activities.GetEnumerator();
    }

    public bool Remove(Activity item)
    {
        if (!_spans.TryGetValue(Activity.Current?.TraceId.ToString(), out var activities))
            return false;
        
        return activities.Remove(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        if (!_spans.TryGetValue(Activity.Current?.TraceId.ToString(), out var activities))
            return Enumerable.Empty<Activity>().GetEnumerator();

        return activities.GetEnumerator();
    }

    public void RemoveAllSpansForTest()
    {
        _spans.Remove(Activity.Current?.TraceId.ToString());
    }

    public bool SpanExistsWithName(string name)
    {
        if (!_spans.TryGetValue(Activity.Current?.TraceId.ToString(), out var activities))
            return false;
        
        return activities.Any(activity => activity.DisplayName.Contains(name));
    }

    public List<Activity> GetSpanByName(string name)
    {
        if (!_spans.TryGetValue(Activity.Current?.TraceId.ToString(), out var activities))
            return [];
        
        return activities.Where(activity => activity.DisplayName.Contains(name)).ToList();

    }

}
public class Tuple<T1, T2>
{
    public T1 Item1 { get; set; }
    public T2 Item2 { get; set; }

    public Tuple()
    {
        Item1 = default(T1);
        Item2 = default(T2);
    }

    public Tuple(T1 item1, T2 item2)
    {
        this.Item1 = item1;
        this.Item2 = item2;
    }
}
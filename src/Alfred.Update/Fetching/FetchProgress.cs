namespace Alfred.Update.Fetching;

public readonly record struct FetchProgress(long CopiedBytes, long TotalBytes)
{
    public double Ratio => TotalBytes <= 0 ? 0d : Math.Clamp((double)CopiedBytes / TotalBytes, 0d, 1d);
}

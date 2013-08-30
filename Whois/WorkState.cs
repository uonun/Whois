namespace Whois
{
    internal enum WorkState
    {
        Inited,
        Generating,
        GeneratingCanceling,
        GeneratingCanceled,
        Generated,
        Checking,
        CheckCanceling,
        CheckCanceled,
        CheckCompleted,
    }
}
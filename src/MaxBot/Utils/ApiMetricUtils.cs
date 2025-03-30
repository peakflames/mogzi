namespace MaxBot.Utils;

public static class ApiMetricUtils
{


    public static int GetSimplisticTokenCount(IEnumerable<ChatMessage> messages)
    {
        const int TokensPerMessage = 3;
        const int TokensPerRole = 1;
        const int BaseTokens = 3;
        var disallowedSpecial = new HashSet<string>();

        var tokenCount = BaseTokens;

        var encoding = SharpToken.GptEncoding.GetEncoding("cl100k_base");
        foreach (var message in messages)
        {
            tokenCount += TokensPerMessage;
            tokenCount += TokensPerRole;
            tokenCount += encoding.Encode(message.Text, disallowedSpecial).Count;
        }

        return tokenCount;
    }
}
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Services;
public class QuestionRewriteService : IQuestionRewriteService
{
    private readonly IRepository<ChatSession> _sessionRepository;
    public QuestionRewriteService(
       IRepository<ChatSession> sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }
    private static readonly string[] PronounPatterns =
    {
        "ông ấy",
        "bà ấy",
        "ông ta",
        "bà ta",
        "ông",
        "bà",
        "người này",
        "người đó",
        "vị này",
        "vị đó",
        "ngài",
        "ngài ấy"
    };

    // Các mẫu câu follow-up ngắn, thiếu chủ ngữ
    private static readonly string[] ShortImplicitPatterns =
    {
        "sinh ở đâu",
        "sinh năm nào",
        "sinh năm bao nhiêu",
        "quê ở đâu",
        "quê quán ở đâu",
        "mất năm nào",
        "mất khi nào",
        "mất ở đâu",
        "chết năm nào",
        "chết khi nào",
        "là ai",
        "là người như thế nào",
        "tên thật là gì",
        "tên đầy đủ là gì",
        "có công lao gì",
        "đóng góp gì",
        "nổi tiếng vì gì",
        "làm gì",
        "làm được gì",
        "giữ chức vụ gì",
        "thuộc thời nào",
        "sống vào thời nào",
        "cha là ai",
        "mẹ là ai",
        "cha mẹ là ai",
        "vợ là ai",
        "chồng là ai"
    };

    public async Task<string> RewriteQuestion(string question, Guid sessionId)
    {

        if (string.IsNullOrWhiteSpace(question))
        {
            return question;
        }

        var sessions = await _sessionRepository.FindAsync(x => x.SessionId == sessionId);
        var session = sessions.FirstOrDefault();
        if (session == null) 
        {
            return question;
        }
        var activePerson = session.ActivePerson;

        var normalized = NormalizeSpaces(question);

        // Nếu câu đã có tên người rõ ràng thì giữ nguyên
        var detectedPerson = ExtractPersonName(normalized);
        if (!string.IsNullOrWhiteSpace(detectedPerson))
        {
            return normalized;
        }

        if (string.IsNullOrWhiteSpace(activePerson))
        {
            return normalized;
        }

        // Case 1: có đại từ => replace
        if (IsFollowUpPronounQuestion(normalized))
        {
            var rewrittenByPronoun = ReplacePronounsWithActivePerson(normalized, activePerson);
            rewrittenByPronoun = CleanupQuestion(rewrittenByPronoun);

            return rewrittenByPronoun;
        }

        // Case 2: câu follow-up ngắn, thiếu chủ ngữ => prepend active person
        if (IsShortImplicitFollowUpQuestion(normalized))
        {
            var rewrittenImplicit = PrependActivePerson(normalized, activePerson);
            rewrittenImplicit = CleanupQuestion(rewrittenImplicit);

            return rewrittenImplicit;
        }

        return normalized;
    }

    public bool IsFollowUpPronounQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        var q = NormalizeSpaces(question).ToLowerInvariant();
        return PronounPatterns.Any(p => q.Contains(p));
    }

    public bool IsShortImplicitFollowUpQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        var q = NormalizeForComparison(question);

        // Không coi là follow-up ngắn nếu đã có tên người
        if (!string.IsNullOrWhiteSpace(ExtractPersonName(question)))
        {
            return false;
        }

        // Match trực tiếp pattern
        if (ShortImplicitPatterns.Any(p => q == p || q.StartsWith(p + " ")))
        {
            return true;
        }

        // Một số câu ngắn có dấu hỏi và ít từ, ví dụ:
        // "sinh ở đâu?", "quê ở đâu?", "mất năm nào?"
        var wordCount = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount <= 6)
        {
            if (q.StartsWith("sinh ") ||
                q.StartsWith("quê ") ||
                q.StartsWith("mất ") ||
                q.StartsWith("chết ") ||
                q.StartsWith("tên ") ||
                q.StartsWith("cha ") ||
                q.StartsWith("mẹ ") ||
                q.StartsWith("vợ ") ||
                q.StartsWith("chồng ") ||
                q.StartsWith("đóng góp ") ||
                q.StartsWith("có công ") ||
                q.StartsWith("làm gì") ||
                q.StartsWith("giữ chức"))
            {
                return true;
            }
        }

        return false;
    }

    public string? ExtractPersonName(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        var input = NormalizeSpaces(question);

        var matches = Regex.Matches(
            input,
            @"\b([A-ZÀ-Ỹ][a-zà-ỹ]+(?:\s+[A-ZÀ-Ỹ][a-zà-ỹ]+){1,4})\b");

        if (matches.Count == 0)
        {
            return null;
        }

        var candidate = matches[0].Value.Trim();

        if (candidate.Length < 5)
        {
            return null;
        }

        return candidate;
    }

    private static string ReplacePronounsWithActivePerson(string question, string activePerson)
    {
        var rewritten = question;

        rewritten = Regex.Replace(rewritten, @"\bông ấy\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bbà ấy\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bông ta\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bbà ta\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bngười này\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bngười đó\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bvị này\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bvị đó\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bông\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\bbà\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\ngài\b", activePerson, RegexOptions.IgnoreCase);
        rewritten = Regex.Replace(rewritten, @"\ngài ấy\b", activePerson, RegexOptions.IgnoreCase);
        return rewritten;
    }

    private static string PrependActivePerson(string question, string activePerson)
    {
        var q = question.Trim();

        // viết thường chữ đầu nếu user gõ kiểu "Sinh ở đâu?"
        q = LowercaseFirstCharacterIfNeeded(q);

        return $"{activePerson} {q}";
    }

    private static string NormalizeSpaces(string input)
    {
        return Regex.Replace(input.Trim(), @"\s+", " ");
    }

    private static string NormalizeForComparison(string input)
    {
        var text = NormalizeSpaces(input).ToLowerInvariant();
        text = text.TrimEnd('?', '.', '!', ',');
        return text;
    }

    private static string CleanupQuestion(string input)
    {
        var text = NormalizeSpaces(input);

        text = Regex.Replace(text, @"\s+\?", "?");
        text = Regex.Replace(text, @"\s+\.", ".");
        text = Regex.Replace(text, @"\s+,", ",");
        text = Regex.Replace(text, @"\s+!", "!");

        return text;
    }

    private static string LowercaseFirstCharacterIfNeeded(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        if (input.Length == 1)
        {
            return input.ToLowerInvariant();
        }

        return char.ToLowerInvariant(input[0]) + input[1..];
    }
}
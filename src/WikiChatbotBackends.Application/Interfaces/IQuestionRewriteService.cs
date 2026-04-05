using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiChatbotBackends.Application.Interfaces
{
    public interface IQuestionRewriteService
    {
        Task<string> RewriteQuestion(string question,Guid sessionId);
        string? ExtractPersonName(string question);
        bool IsFollowUpPronounQuestion(string question);
        bool IsShortImplicitFollowUpQuestion(string question);
    }
}

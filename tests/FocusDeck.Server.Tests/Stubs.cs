using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Jarvis;

namespace FocusDeck.Server.Tests
{
    internal sealed class StubSuggestionService : ISuggestionService
    {
        public Task<SuggestionResponseDto?> GenerateSuggestionAsync(SuggestionRequestDto request)
        {
            return Task.FromResult<SuggestionResponseDto?>(new SuggestionResponseDto("test_action", new(), 0.9, new[] { Guid.NewGuid() }));
        }

        public Task<List<NoteSuggestion>> AnalyzeNoteAsync(Note note)
        {
            return Task.FromResult(new List<NoteSuggestion>());
        }
    }

    internal sealed class StubFeedbackService : IFeedbackService
    {
        public Task ProcessFeedbackAsync(FeedbackRequestDto request)
        {
            return Task.CompletedTask;
        }
    }
}

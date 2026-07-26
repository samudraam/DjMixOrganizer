// File location: DjMixOrganizer.App/ViewModels/MixDetailViewModel.cs
//
// TEACHING NOTES
// ---------------
// IQueryAttributable is MAUI Shell's way of passing data across a
// navigation boundary without two pages needing a direct reference to each
// other. MixListPage doesn't construct MixDetailViewModel and hand it a
// Mix — it just navigates to a route with a query string
// (GoToAsync($"MixDetailPage?mixId={id}")), and Shell calls
// ApplyQueryAttributes on whatever ViewModel is bound to the page it
// routed to. This is the same decoupling idea as passing an id through a
// URL instead of passing an object through props/navigation state.

using CommunityToolkit.Mvvm.ComponentModel;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.App.ViewModels;

public partial class MixDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IMixRepository _mixRepository;

    [ObservableProperty]
    private Mix? _mix;

    [ObservableProperty]
    private bool _isNewMix = true;

    public MixDetailViewModel(IMixRepository mixRepository)
    {
        _mixRepository = mixRepository;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("mixId", out var idValue) ||
            !Guid.TryParse(idValue?.ToString(), out var mixId))
        {
            IsNewMix = true;
            return;
        }

        IsNewMix = false;
        var mixes = await _mixRepository.GetAllAsync();
        Mix = mixes.FirstOrDefault(m => m.Id == mixId);
    }
}

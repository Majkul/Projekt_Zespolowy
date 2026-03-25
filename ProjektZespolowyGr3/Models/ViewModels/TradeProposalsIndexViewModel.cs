using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.ViewModels;

/// <summary>
/// Podsumowanie listy „Moje wymiany”: po jednej propozycji na ogłoszenie w każdej sekcji (szczegóły = pełna historia).
/// </summary>
public class TradeProposalsIndexViewModel
{
    public int CurrentUserId { get; set; }
    /// <summary>Propozycje w toku (Pending), najświeższa na ogłoszenie.</summary>
    public IReadOnlyList<TradeProposal> ActiveOnePerListing { get; set; } = Array.Empty<TradeProposal>();
    /// <summary>Propozycje zakończone, tylko dla ogłoszeń bez żadnego Pending; najświeższa na ogłoszenie.</summary>
    public IReadOnlyList<TradeProposal> ArchivedOnePerListing { get; set; } = Array.Empty<TradeProposal>();
}

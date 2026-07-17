using ChipGauge.App.Services;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// A data-driven page for one hardware category (CPU, GPU, Memory…). It lists every
/// device of its <see cref="Kind"/> with identification and live sensors — so all
/// category pages share one view and one code path.
/// </summary>
public sealed class CategoryViewModel : PageViewModel
{
    public CategoryViewModel(
        Localizer localizer,
        string titleKey,
        string iconKey,
        HardwareKind kind,
        MonitoringEngine engine)
        : base(localizer, titleKey, iconKey)
    {
        ArgumentNullException.ThrowIfNull(engine);
        Kind = kind;
        Items =
        [
            .. engine.Hardware
                .Where(h => h.Kind == kind)
                .Select(h => new HardwareItemViewModel(h, engine.History)),
        ];
    }

    /// <summary>The category this page presents.</summary>
    public HardwareKind Kind { get; }

    /// <summary>The devices in this category.</summary>
    public IReadOnlyList<HardwareItemViewModel> Items { get; }

    /// <summary>True when at least one device exists in this category.</summary>
    public bool HasData => Items.Count > 0;

    /// <inheritdoc />
    public override void OnTick()
    {
        foreach (HardwareItemViewModel item in Items)
        {
            item.Refresh();
        }
    }
}

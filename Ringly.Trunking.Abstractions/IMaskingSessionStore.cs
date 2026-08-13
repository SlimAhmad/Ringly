using Ringly.Trunking.Abstractions.Models;

namespace Ringly.Trunking.Abstractions;

// §8.3 — not doc-specified as an interface anywhere (the orchestration example just calls
// `this.maskingSessionStore.RetrieveByMaskedNumberAsync(...)`); this is the minimal contract
// that example needs. No concrete store ships with this row — that's a separate concern,
// consistent with how orchestration services depend on abstractions only.
public interface IMaskingSessionStore
{
    ValueTask<MaskingSession?> RetrieveByMaskedNumberAsync(string maskedNumber);
}

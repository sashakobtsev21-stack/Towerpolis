using System;
using Towerpolis.Core.Meta;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Cloud-save seam (ADR-0009): the local file at persistentDataPath stays the source of truth; a cloud
    /// backend is a sync target. <see cref="LocalCloudSave"/> (the default) is a no-op, so today's behaviour
    /// is unchanged. In Phase 7, after Google Play Games sign-in, set <see cref="CloudSave.Backend"/> to a
    /// <c>GpgsCloudSave</c> — no save call site changes. <see cref="SaveData"/> is unchanged (string-free Core).
    /// </summary>
    public interface ICloudSave
    {
        /// <summary>True once a real backend is signed in and ready (the stub is always false).</summary>
        bool IsAvailable { get; }

        /// <summary>Upload the latest local save (fire-and-forget; a real impl may queue/throttle).</summary>
        void Push(SaveData data);

        /// <summary>Download the cloud save asynchronously; calls back with null if none/unavailable.</summary>
        void Pull(Action<SaveData> onResult);
    }

    /// <summary>Default no-op backend: cloud disabled, local file is the only store (today's behaviour).</summary>
    public sealed class LocalCloudSave : ICloudSave
    {
        public bool IsAvailable => false;
        public void Push(SaveData data) { /* no-op until a real backend is wired (Phase 7) */ }
        public void Pull(Action<SaveData> onResult) => onResult?.Invoke(null);
    }

    /// <summary>Holds the active cloud backend. Defaults to the no-op stub; swap at boot when GPGS is added.</summary>
    public static class CloudSave
    {
        public static ICloudSave Backend { get; set; } = new LocalCloudSave();
    }
}

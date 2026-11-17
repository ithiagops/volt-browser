(enabled, background, revision) => {
    let state = window.__voltMediaPolicy;
    if (!state) {
        state = { enabled: false, background: false, revision: -1 };
        window.__voltMediaPolicy = state;
        const shouldPause = () => state.enabled && (state.background || document.hidden);
        const pauseAll = () => {
            if (!shouldPause()) return;
            for (const media of document.querySelectorAll('video,audio')) {
                try { if (!media.paused) media.pause(); } catch (_) { }
            }
        };
        document.addEventListener('play', event => {
            if (shouldPause() && event.target && typeof event.target.pause === 'function') {
                try { event.target.pause(); } catch (_) { }
            }
        }, true);
        document.addEventListener('visibilitychange', pauseAll);
        document.addEventListener('DOMContentLoaded', pauseAll, { once: true });
        state.pauseAll = pauseAll;
        state.update = (nextEnabled, nextBackground, nextRevision) => {
            if (nextRevision < state.revision) return;
            state.enabled = nextEnabled; state.background = nextBackground; state.revision = nextRevision;
            pauseAll();
        };
    }
    // Uma atualização atrasada de outra troca de aba não pode reverter a atual.
    state.update(enabled, background, revision);
}

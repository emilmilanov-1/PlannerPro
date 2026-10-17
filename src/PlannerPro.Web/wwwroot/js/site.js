document.addEventListener("submit", event => {
    const form = event.target.closest("form[data-loading]");
    if (!form || !form.checkValidity()) {
        return;
    }

    window.setTimeout(() => {
        if (event.defaultPrevented) {
            return;
        }

        const submitButton = event.submitter ?? form.querySelector('button[type="submit"]');
        if (!submitButton) {
            return;
        }

        form.setAttribute("aria-busy", "true");
        submitButton.disabled = true;
        submitButton.innerHTML = `<span class="button-spinner" aria-hidden="true"></span>${form.dataset.loadingText ?? "Обработване…"}`;
    }, 0);
});

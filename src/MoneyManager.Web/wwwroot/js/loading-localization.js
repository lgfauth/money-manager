// Loading screen localization
// This runs before Blazor loads, so it uses localStorage to detect preferred language

(function () {
    const translations = {
        'pt-BR': {
            appName: 'MoneyManager',
            loading: 'Carregando aplica\u00E7\u00E3o...',
            errorTitle: 'Uma exce\u00E7\u00E3o n\u00E3o tratada ocorreu.',
            errorDetails: 'Veja o navegador dev tools para detalhes.',
            reload: 'Recarregar',
            dismiss: '\u2716'
        }
    };

    function detectLanguage() {
        // App now supports only pt-BR.
        return 'pt-BR';
    }

    function applyTranslations() {
        const lang = detectLanguage();
        const texts = translations[lang];

        // Update loading text
        const loadingText = document.querySelector('.loading-container p');
        if (loadingText) {
            loadingText.textContent = texts.loading;
        }

        // Update error UI (in case it's already visible)
        updateErrorUI(texts);
    }

    function updateErrorUI(texts) {
        const errorUI = document.getElementById('blazor-error-ui');
        if (errorUI) {
            const errorText = errorUI.childNodes[0];
            if (errorText && errorText.nodeType === Node.TEXT_NODE) {
                errorText.textContent = texts.errorTitle + ' ' + texts.errorDetails + ' ';
            }

            const reloadLink = errorUI.querySelector('.reload');
            if (reloadLink) {
                reloadLink.textContent = texts.reload;
            }

            const dismissLink = errorUI.querySelector('.dismiss');
            if (dismissLink) {
                dismissLink.textContent = texts.dismiss;
            }
        }
    }

    // Apply translations when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyTranslations);
    } else {
        applyTranslations();
    }

    // Export for use by Blazor later
    window.loadingLocalization = {
        getCurrentLanguage: detectLanguage,
        getTranslations: function (lang) {
            return translations[lang] || translations['pt-BR'];
        }
    };
})();

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
        },
        'en-US': {
            appName: 'MoneyManager',
            loading: 'Loading application...',
            errorTitle: 'An unhandled exception occurred.',
            errorDetails: 'See browser dev tools for details.',
            reload: 'Reload',
            dismiss: '\u2716'
        },
        'es-ES': {
            appName: 'MoneyManager',
            loading: 'Cargando aplicaci\u00F3n...',
            errorTitle: 'Ocurri\u00F3 una excepci\u00F3n no controlada.',
            errorDetails: 'Consulte las herramientas de desarrollo del navegador para obtener m\u00E1s detalles.',
            reload: 'Recargar',
            dismiss: '\u2716'
        },
        'fr-FR': {
            appName: 'MoneyManager',
            loading: 'Chargement de l\'application...',
            errorTitle: 'Une exception non g\u00E9r\u00E9e s\'est produite.',
            errorDetails: 'Voir les outils de d\u00E9veloppement du navigateur pour plus de d\u00E9tails.',
            reload: 'Recharger',
            dismiss: '\u2716'
        }
    };

    function detectLanguage() {
        // 1. Try localStorage (user preference)
        try {
            const stored = localStorage.getItem('preferred_language');
            if (stored && translations[stored]) {
                return stored;
            }
        } catch (e) {
            // ignore
        }

        // 2. Try browser language
        const browserLang = navigator.language || navigator.userLanguage;
        if (translations[browserLang]) {
            return browserLang;
        }

        // 3. Try browser language without region (pt, en, es, fr)
        const shortLang = browserLang.split('-')[0];
        const matchingKey = Object.keys(translations).find(key => key.startsWith(shortLang));
        if (matchingKey) {
            return matchingKey;
        }

        // 4. Default to pt-BR
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

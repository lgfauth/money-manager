// Loading screen localization
// This runs before Blazor loads, so it uses localStorage to detect preferred language

(function () {
    const translations = {
        'pt-BR': {
            appName: 'MoneyManager',
            loading: 'Carregando aplicação...',
            errorTitle: 'Uma exceção não tratada ocorreu.',
            errorDetails: 'Veja o navegador dev tools para detalhes.',
            reload: 'Recarregar',
            dismiss: '?'
        },
        'en-US': {
            appName: 'MoneyManager',
            loading: 'Loading application...',
            errorTitle: 'An unhandled exception occurred.',
            errorDetails: 'See browser dev tools for details.',
            reload: 'Reload',
            dismiss: '?'
        },
        'es-ES': {
            appName: 'MoneyManager',
            loading: 'Cargando aplicación...',
            errorTitle: 'Ocurrió una excepción no controlada.',
            errorDetails: 'Consulte las herramientas de desarrollo del navegador para obtener más detalles.',
            reload: 'Recargar',
            dismiss: '?'
        },
        'fr-FR': {
            appName: 'MoneyManager',
            loading: 'Chargement de l\'application...',
            errorTitle: 'Une exception non gérée s\'est produite.',
            errorDetails: 'Voir les outils de développement du navigateur pour plus de détails.',
            reload: 'Recharger',
            dismiss: '?'
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

// Badge Contrast Helper
window.badgeHelper = {
    // Calculate luminance from RGB
    getLuminance: function(r, g, b) {
        const a = [r, g, b].map(function(v) {
            v /= 255;
            return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
        });
        return a[0] * 0.2126 + a[1] * 0.7152 + a[2] * 0.0722;
    },
    
    // Calculate contrast ratio
    getContrastRatio: function(luminance1, luminance2) {
        const brightest = Math.max(luminance1, luminance2);
        const darkest = Math.min(luminance1, luminance2);
        return (brightest + 0.05) / (darkest + 0.05);
    },
    
    // Get text color based on background
    getTextColor: function(backgroundColor) {
        // Parse color
        const rgb = this.parseColor(backgroundColor);
        if (!rgb) return '#000000';
        
        const bgLuminance = this.getLuminance(rgb.r, rgb.g, rgb.b);
        const whiteLuminance = 1; // White
        const blackLuminance = 0; // Black
        
        const whiteContrast = this.getContrastRatio(bgLuminance, whiteLuminance);
        const blackContrast = this.getContrastRatio(bgLuminance, blackLuminance);
        
        // Return color with better contrast (WCAG AA requires 4.5:1)
        return whiteContrast > blackContrast ? '#ffffff' : '#000000';
    },
    
    // Parse color string to RGB
    parseColor: function(color) {
        // Remove spaces
        color = color.replace(/\s/g, '');
        
        // RGB/RGBA
        const rgbMatch = color.match(/rgba?\((\d+),(\d+),(\d+)/i);
        if (rgbMatch) {
            return {
                r: parseInt(rgbMatch[1]),
                g: parseInt(rgbMatch[2]),
                b: parseInt(rgbMatch[3])
            };
        }
        
        // Hex
        const hexMatch = color.match(/^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i);
        if (hexMatch) {
            return {
                r: parseInt(hexMatch[1], 16),
                g: parseInt(hexMatch[2], 16),
                b: parseInt(hexMatch[3], 16)
            };
        }
        
        // Short hex
        const shortHexMatch = color.match(/^#?([a-f\d])([a-f\d])([a-f\d])$/i);
        if (shortHexMatch) {
            return {
                r: parseInt(shortHexMatch[1] + shortHexMatch[1], 16),
                g: parseInt(shortHexMatch[2] + shortHexMatch[2], 16),
                b: parseInt(shortHexMatch[3] + shortHexMatch[3], 16)
            };
        }
        
        // Named colors (fallback)
        const tempEl = document.createElement('div');
        tempEl.style.color = color;
        document.body.appendChild(tempEl);
        const computed = window.getComputedStyle(tempEl).color;
        document.body.removeChild(tempEl);
        return this.parseColor(computed);
    },
    
    // Apply contrast to all badges
    applyContrastToBadges: function() {
        const badges = document.querySelectorAll('.badge');
        badges.forEach(badge => {
            const bgColor = window.getComputedStyle(badge).backgroundColor;
            const textColor = this.getTextColor(bgColor);
            badge.style.color = textColor;
        });
    }
};

// Auto-apply on load and mutations
document.addEventListener('DOMContentLoaded', () => {
    badgeHelper.applyContrastToBadges();
    
    // Watch for new badges
    const observer = new MutationObserver(() => {
        badgeHelper.applyContrastToBadges();
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

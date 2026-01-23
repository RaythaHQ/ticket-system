/**
 * SearchableSelect - A beautiful, accessible searchable dropdown component
 * 
 * Usage:
 *   new SearchableSelect(selectElement, {
 *     placeholder: 'Select an option...',
 *     searchPlaceholder: 'Type to search...',
 *     showAvatars: true,
 *     groupBy: null, // or a function to group options
 *     renderOption: null, // custom render function
 *   });
 * 
 * Or use data attributes:
 *   <select data-searchable-select data-placeholder="Choose..." ...>
 */

class SearchableSelect {
    constructor(selectElement, options = {}) {
        if (!selectElement || selectElement.tagName !== 'SELECT') {
            console.error('SearchableSelect: Invalid select element');
            return;
        }

        // Prevent double initialization
        if (selectElement.dataset.searchableSelectInitialized) {
            return;
        }
        selectElement.dataset.searchableSelectInitialized = 'true';

        this.select = selectElement;
        this.options = {
            placeholder: options.placeholder || selectElement.dataset.placeholder || 'Select...',
            searchPlaceholder: options.searchPlaceholder || selectElement.dataset.searchPlaceholder || 'Type to search...',
            showAvatars: options.showAvatars !== undefined ? options.showAvatars : selectElement.dataset.showAvatars !== 'false',
            size: options.size || selectElement.dataset.size || 'normal', // 'normal' or 'sm'
            groupBy: options.groupBy || null,
            renderOption: options.renderOption || null,
            onSelect: options.onSelect || null,
        };

        this.isOpen = false;
        this.highlightedIndex = -1;
        this.filteredOptions = [];

        this.init();
    }

    init() {
        // Hide original select
        this.select.style.display = 'none';

        // Create container
        this.container = document.createElement('div');
        this.container.className = 'searchable-select-container';
        if (this.options.size === 'sm') {
            this.container.classList.add('searchable-select-sm');
        }

        // Create trigger button
        this.trigger = document.createElement('button');
        this.trigger.type = 'button';
        this.trigger.className = 'searchable-select-trigger';
        this.trigger.innerHTML = `
            <span class="searchable-select-value placeholder">${this.escapeHtml(this.options.placeholder)}</span>
            <i class="bi bi-chevron-down searchable-select-arrow"></i>
        `;

        // Create dropdown
        this.dropdown = document.createElement('div');
        this.dropdown.className = 'searchable-select-dropdown';
        this.dropdown.innerHTML = `
            <div class="searchable-select-search-wrapper">
                <input type="text" class="searchable-select-search" placeholder="${this.escapeHtml(this.options.searchPlaceholder)}" autocomplete="off">
            </div>
            <div class="searchable-select-count"></div>
            <div class="searchable-select-options"></div>
        `;

        // Assemble
        this.container.appendChild(this.trigger);
        this.container.appendChild(this.dropdown);
        this.select.parentNode.insertBefore(this.container, this.select.nextSibling);

        // Cache elements
        this.searchInput = this.dropdown.querySelector('.searchable-select-search');
        this.optionsContainer = this.dropdown.querySelector('.searchable-select-options');
        this.countDisplay = this.dropdown.querySelector('.searchable-select-count');

        // Bind events
        this.bindEvents();

        // Initial render
        this.updateOptions();
        this.updateDisplay();
    }

    bindEvents() {
        // Toggle dropdown
        this.trigger.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.toggle();
        });

        // Search input
        this.searchInput.addEventListener('input', () => {
            this.filterOptions(this.searchInput.value);
        });

        // Prevent form submission on enter in search
        this.searchInput.addEventListener('keydown', (e) => {
            this.handleKeydown(e);
        });

        // Click outside to close
        document.addEventListener('click', (e) => {
            if (!this.container.contains(e.target)) {
                this.close();
            }
        });

        // Option selection via delegation
        this.optionsContainer.addEventListener('click', (e) => {
            const option = e.target.closest('.searchable-select-option');
            if (option && !option.classList.contains('disabled')) {
                this.selectOption(option.dataset.value);
            }
        });

        // Mouse hover for highlighting
        this.optionsContainer.addEventListener('mousemove', (e) => {
            const option = e.target.closest('.searchable-select-option');
            if (option && !option.classList.contains('disabled')) {
                this.highlightOption(option);
            }
        });

        // Sync if original select changes
        this.select.addEventListener('change', () => {
            this.updateDisplay();
        });
    }

    handleKeydown(e) {
        const visibleOptions = this.optionsContainer.querySelectorAll('.searchable-select-option:not(.disabled)');

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.highlightedIndex = Math.min(this.highlightedIndex + 1, visibleOptions.length - 1);
                this.updateHighlight(visibleOptions);
                break;
            case 'ArrowUp':
                e.preventDefault();
                this.highlightedIndex = Math.max(this.highlightedIndex - 1, 0);
                this.updateHighlight(visibleOptions);
                break;
            case 'Enter':
                e.preventDefault();
                if (this.highlightedIndex >= 0 && visibleOptions[this.highlightedIndex]) {
                    this.selectOption(visibleOptions[this.highlightedIndex].dataset.value);
                }
                break;
            case 'Escape':
                e.preventDefault();
                this.close(true); // Return focus to trigger on Escape
                break;
            case 'Tab':
                this.close(); // Don't steal focus on Tab - let browser handle it
                break;
        }
    }

    updateHighlight(visibleOptions) {
        visibleOptions.forEach((opt, i) => {
            opt.classList.toggle('highlighted', i === this.highlightedIndex);
        });

        // Scroll into view
        if (visibleOptions[this.highlightedIndex]) {
            visibleOptions[this.highlightedIndex].scrollIntoView({
                block: 'nearest',
                behavior: 'smooth'
            });
        }
    }

    highlightOption(optionEl) {
        const visibleOptions = Array.from(this.optionsContainer.querySelectorAll('.searchable-select-option:not(.disabled)'));
        this.highlightedIndex = visibleOptions.indexOf(optionEl);
        visibleOptions.forEach((opt, i) => {
            opt.classList.toggle('highlighted', i === this.highlightedIndex);
        });
    }

    toggle() {
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
    }

    open() {
        this.isOpen = true;
        this.trigger.classList.add('open');
        this.dropdown.classList.add('show');
        this.searchInput.value = '';
        this.filterOptions('');
        this.highlightedIndex = -1;

        // Check if dropdown would go off-screen and flip if needed
        this.positionDropdown();

        // Focus search input after a brief delay for animation
        setTimeout(() => {
            this.searchInput.focus();
        }, 50);
    }

    positionDropdown() {
        // Reset any previous positioning
        this.dropdown.style.top = '';
        this.dropdown.style.bottom = '';
        this.container.classList.remove('dropdown-above');
        this.trigger.style.borderRadius = '';

        const triggerRect = this.trigger.getBoundingClientRect();
        const dropdownHeight = this.dropdown.offsetHeight || 350; // Estimate if not yet rendered
        const viewportHeight = window.innerHeight;
        const spaceBelow = viewportHeight - triggerRect.bottom;
        const spaceAbove = triggerRect.top;

        // If not enough space below and more space above, flip the dropdown
        if (spaceBelow < dropdownHeight && spaceAbove > spaceBelow) {
            this.container.classList.add('dropdown-above');
            this.dropdown.style.bottom = '100%';
            this.dropdown.style.top = 'auto';
            this.trigger.style.borderRadius = '0 0 0.5rem 0.5rem';
        }
    }

    close(returnFocus = false) {
        this.isOpen = false;
        this.trigger.classList.remove('open');
        this.dropdown.classList.remove('show');
        this.container.classList.remove('dropdown-above');
        this.trigger.style.borderRadius = '';
        this.dropdown.style.top = '';
        this.dropdown.style.bottom = '';
        // Only focus trigger if explicitly requested (e.g., Escape key)
        // Don't steal focus when clicking outside - let the clicked element keep focus
        if (returnFocus) {
            this.trigger.focus();
        }
    }

    updateOptions() {
        this.allOptions = Array.from(this.select.options).map(opt => ({
            value: opt.value,
            text: opt.textContent.trim(),
            disabled: opt.disabled,
            group: opt.dataset.group || opt.parentElement.tagName === 'OPTGROUP' ? opt.parentElement.label : null,
            avatar: opt.dataset.avatar || null,
            avatarType: opt.dataset.avatarType || 'user',
            subtext: opt.dataset.subtext || null,
        }));

        this.filteredOptions = [...this.allOptions];
    }

    filterOptions(query) {
        const q = query.toLowerCase().trim();

        if (!q) {
            this.filteredOptions = [...this.allOptions];
        } else {
            this.filteredOptions = this.allOptions.filter(opt =>
                opt.text.toLowerCase().includes(q) ||
                (opt.subtext && opt.subtext.toLowerCase().includes(q))
            );
        }

        this.renderOptions(q);
        this.highlightedIndex = -1;
    }

    renderOptions(query = '') {
        const groups = {};
        let ungrouped = [];

        // Group options
        this.filteredOptions.forEach(opt => {
            if (opt.group) {
                if (!groups[opt.group]) groups[opt.group] = [];
                groups[opt.group].push(opt);
            } else {
                ungrouped.push(opt);
            }
        });

        let html = '';
        const selectedValue = this.select.value;

        // Render ungrouped first
        ungrouped.forEach(opt => {
            html += this.renderOptionHtml(opt, selectedValue, query);
        });

        // Render groups
        Object.keys(groups).forEach(groupName => {
            html += `<div class="searchable-select-group">${this.escapeHtml(groupName)}</div>`;
            groups[groupName].forEach(opt => {
                html += this.renderOptionHtml(opt, selectedValue, query);
            });
        });

        if (!html) {
            html = `
                <div class="searchable-select-no-results">
                    <i class="bi bi-search"></i>
                    No matches found
                </div>
            `;
        }

        this.optionsContainer.innerHTML = html;

        // Update count
        const count = this.filteredOptions.length;
        const total = this.allOptions.length;
        if (query && count !== total) {
            this.countDisplay.textContent = `${count} of ${total} results`;
            this.countDisplay.style.display = 'block';
        } else {
            this.countDisplay.style.display = 'none';
        }
    }

    renderOptionHtml(opt, selectedValue, query) {
        if (this.options.renderOption) {
            return this.options.renderOption(opt, selectedValue, query);
        }

        const isSelected = opt.value === selectedValue;
        const avatarHtml = this.options.showAvatars ? this.renderAvatar(opt) : '';
        const highlightedText = query ? this.highlightMatch(opt.text, query) : this.escapeHtml(opt.text);
        const subtextHtml = opt.subtext ? `<div class="searchable-select-option-subtext">${this.escapeHtml(opt.subtext)}</div>` : '';

        return `
            <div class="searchable-select-option ${isSelected ? 'selected' : ''} ${opt.disabled ? 'disabled' : ''}" 
                 data-value="${this.escapeHtml(opt.value)}">
                ${avatarHtml}
                <div class="searchable-select-option-content">
                    <div class="searchable-select-option-text">${highlightedText}</div>
                    ${subtextHtml}
                </div>
            </div>
        `;
    }

    renderAvatar(opt) {
        // Check for special values
        const val = opt.value.toLowerCase();
        if (val === '' || val === 'unassigned' || opt.text.toLowerCase().includes('unassigned')) {
            return `<div class="searchable-select-option-avatar unassigned"><i class="bi bi-person-dash"></i></div>`;
        }

        // Team check
        if (val.startsWith('team:') && !val.includes('assignee:')) {
            const initial = opt.text.replace(/^[^a-zA-Z]*/, '').charAt(0).toUpperCase() || 'T';
            return `<div class="searchable-select-option-avatar team">${initial}</div>`;
        }

        // Default user avatar
        const initial = opt.text.replace(/^[^a-zA-Z]*/, '').charAt(0).toUpperCase() || '?';
        return `<div class="searchable-select-option-avatar">${initial}</div>`;
    }

    highlightMatch(text, query) {
        if (!query) return this.escapeHtml(text);

        const escaped = this.escapeHtml(text);
        const q = query.toLowerCase();
        const lowerText = text.toLowerCase();
        const index = lowerText.indexOf(q);

        if (index === -1) return escaped;

        const before = this.escapeHtml(text.substring(0, index));
        const match = this.escapeHtml(text.substring(index, index + query.length));
        const after = this.escapeHtml(text.substring(index + query.length));

        return `${before}<span class="searchable-select-match">${match}</span>${after}`;
    }

    selectOption(value) {
        this.select.value = value;
        this.select.dispatchEvent(new Event('change', { bubbles: true }));

        this.updateDisplay();
        this.close();

        if (this.options.onSelect) {
            this.options.onSelect(value, this.allOptions.find(o => o.value === value));
        }
    }

    updateDisplay() {
        const selectedOption = this.select.options[this.select.selectedIndex];
        const valueEl = this.trigger.querySelector('.searchable-select-value');

        // Check if there's a selected option with text content
        // Even if value is empty, if there's text, show it (e.g., "All Assignees")
        if (selectedOption && selectedOption.textContent.trim()) {
            valueEl.textContent = selectedOption.textContent.trim();
            valueEl.classList.remove('placeholder');
        } else {
            valueEl.textContent = this.options.placeholder;
            valueEl.classList.add('placeholder');
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Public methods
    refresh() {
        this.updateOptions();
        this.updateDisplay();
    }

    setValue(value) {
        this.select.value = value;
        this.updateDisplay();
    }

    getValue() {
        return this.select.value;
    }

    destroy() {
        this.container.remove();
        this.select.style.display = '';
    }
}

// Auto-initialize on elements with data-searchable-select attribute
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('select[data-searchable-select]').forEach(select => {
        new SearchableSelect(select);
    });
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SearchableSelect;
}


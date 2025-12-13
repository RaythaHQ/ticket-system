/**
 * Multi-Level Sort Configurator Component
 * Provides drag-and-drop sortable sort levels for ticket views.
 */
class SortConfigurator {
    constructor(container, options = {}) {
        this.container = typeof container === 'string' 
            ? document.querySelector(container) 
            : container;
        
        if (!this.container) {
            console.error('SortConfigurator: Container not found');
            return;
        }

        this.options = {
            maxLevels: 6,
            namePrefix: 'SortLevels',
            onAdd: null,
            onRemove: null,
            onChange: null,
            ...options
        };

        // Sortable fields passed via data attribute
        this.fields = JSON.parse(this.container.dataset.sortFields || '[]');

        this.init();
    }

    init() {
        this.sortList = this.container.querySelector('.sort-levels');
        this.addBtn = this.container.querySelector('[data-add-sort-level]');

        if (this.addBtn) {
            this.addBtn.addEventListener('click', () => this.addLevel());
        }

        // Initialize SortableJS if available
        if (window.Sortable && this.sortList) {
            this.sortable = new Sortable(this.sortList, {
                handle: '.drag-handle',
                animation: 150,
                ghostClass: 'sort-level-ghost',
                onEnd: () => this.updateIndices()
            });
        }

        // Initialize existing levels
        this.sortList?.querySelectorAll('.sort-level').forEach((row, index) => {
            this.initLevelRow(row, index);
        });

        this.updateIndices();
    }

    addLevel(values = {}) {
        const levels = this.sortList.querySelectorAll('.sort-level');
        if (levels.length >= this.options.maxLevels) {
            alert(`Maximum ${this.options.maxLevels} sort levels allowed.`);
            return;
        }

        const index = levels.length;
        const row = this.createLevelRow(index, values);
        this.sortList.appendChild(row);
        this.initLevelRow(row, index);
        this.updateAddButtonState();

        if (this.options.onAdd) {
            this.options.onAdd({ index, row });
        }
    }

    createLevelRow(index, values = {}) {
        const row = document.createElement('div');
        row.className = 'sort-level d-flex gap-2 align-items-center mb-2 p-2 bg-light rounded';
        row.dataset.index = index;

        const prefix = `${this.options.namePrefix}[${index}]`;
        const direction = values.direction || 'asc';

        row.innerHTML = `
            <span class="drag-handle text-muted cursor-move" title="Drag to reorder">
                <i class="fas fa-grip-vertical"></i>
            </span>
            
            <input type="hidden" name="${prefix}.Order" class="order-input" value="${index}">
            
            <select name="${prefix}.Field" class="form-select form-select-sm field-select" style="flex: 1;">
                <option value="">Select field...</option>
                ${this.fields.map(f => `
                    <option value="${f.field}" ${values.field === f.field ? 'selected' : ''}>
                        ${f.label}
                    </option>
                `).join('')}
            </select>
            
            <button type="button" class="btn btn-sm btn-outline-secondary direction-toggle" data-direction="${direction}" title="Toggle sort direction">
                <i class="fas fa-arrow-${direction === 'desc' ? 'down' : 'up'}"></i>
                <span class="ms-1">${direction === 'desc' ? 'DESC' : 'ASC'}</span>
            </button>
            <input type="hidden" name="${prefix}.Direction" class="direction-input" value="${direction}">
            
            <button type="button" class="btn btn-sm btn-outline-danger remove-level" title="Remove sort level">
                <i class="fas fa-times"></i>
            </button>
        `;

        return row;
    }

    initLevelRow(row, index) {
        const directionToggle = row.querySelector('.direction-toggle');
        const directionInput = row.querySelector('.direction-input');
        const removeBtn = row.querySelector('.remove-level');

        // Direction toggle handler
        directionToggle.addEventListener('click', () => {
            const current = directionToggle.dataset.direction;
            const newDirection = current === 'asc' ? 'desc' : 'asc';
            
            directionToggle.dataset.direction = newDirection;
            directionInput.value = newDirection;
            
            const icon = directionToggle.querySelector('i');
            icon.className = `fas fa-arrow-${newDirection === 'desc' ? 'down' : 'up'}`;
            
            const label = directionToggle.querySelector('span');
            label.textContent = newDirection === 'desc' ? 'DESC' : 'ASC';

            if (this.options.onChange) {
                this.options.onChange();
            }
        });

        // Remove handler
        removeBtn.addEventListener('click', () => {
            this.removeLevel(row);
        });
    }

    removeLevel(row) {
        row.remove();
        this.updateIndices();
        this.updateAddButtonState();

        if (this.options.onRemove) {
            this.options.onRemove();
        }
    }

    updateIndices() {
        const levels = this.sortList.querySelectorAll('.sort-level');
        levels.forEach((row, index) => {
            row.dataset.index = index;
            
            // Update order input
            const orderInput = row.querySelector('.order-input');
            if (orderInput) {
                orderInput.value = index;
            }

            // Update all name attributes
            row.querySelectorAll('[name]').forEach(input => {
                input.name = input.name.replace(/\[\d+\]/, `[${index}]`);
            });
        });

        if (this.options.onChange) {
            this.options.onChange();
        }
    }

    updateAddButtonState() {
        const count = this.sortList.querySelectorAll('.sort-level').length;
        if (this.addBtn) {
            this.addBtn.disabled = count >= this.options.maxLevels;
        }
    }

    // Get current sort configuration
    getSortLevels() {
        const levels = [];

        this.sortList.querySelectorAll('.sort-level').forEach((row, index) => {
            const field = row.querySelector('.field-select')?.value;
            const direction = row.querySelector('.direction-input')?.value || 'asc';

            if (field) {
                levels.push({ order: index, field, direction });
            }
        });

        return levels;
    }

    // Get formatted display string
    getDisplayString() {
        return this.getSortLevels()
            .map(l => {
                const fieldDef = this.fields.find(f => f.field === l.field);
                const label = fieldDef?.label || l.field;
                const arrow = l.direction === 'desc' ? '↓' : '↑';
                return `${label} ${arrow}`;
            })
            .join(', ');
    }
}

// Auto-initialize sort configurators on page load
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-sort-configurator]').forEach(container => {
        new SortConfigurator(container);
    });
});

// Export for module use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SortConfigurator;
}


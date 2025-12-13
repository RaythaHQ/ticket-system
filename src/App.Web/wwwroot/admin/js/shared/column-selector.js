/**
 * Column Selector Component
 * Provides drag-and-drop sortable column selection for ticket views.
 */
class ColumnSelector {
    constructor(container, options = {}) {
        this.container = typeof container === 'string' 
            ? document.querySelector(container) 
            : container;
        
        if (!this.container) {
            console.error('ColumnSelector: Container not found');
            return;
        }

        this.options = {
            maxColumns: 20,
            namePrefix: 'VisibleColumns',
            onSelect: null,
            onDeselect: null,
            onChange: null,
            ...options
        };

        // Available columns passed via data attribute
        this.columns = JSON.parse(this.container.dataset.columns || '[]');

        this.init();
    }

    init() {
        this.columnList = this.container.querySelector('.column-list');
        this.selectedList = this.container.querySelector('.selected-columns');

        // Initialize SortableJS for selected columns if available
        if (window.Sortable && this.selectedList) {
            this.sortable = new Sortable(this.selectedList, {
                handle: '.drag-handle',
                animation: 150,
                ghostClass: 'column-item-ghost',
                onEnd: () => this.updateOrder()
            });
        }

        // Initialize checkboxes
        this.container.querySelectorAll('.column-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                if (e.target.checked) {
                    this.selectColumn(e.target.value);
                } else {
                    this.deselectColumn(e.target.value);
                }
            });
        });

        // Initialize existing selected columns
        this.updateOrder();
    }

    selectColumn(field) {
        const selectedCount = this.selectedList.querySelectorAll('.selected-column').length;
        if (selectedCount >= this.options.maxColumns) {
            alert(`Maximum ${this.options.maxColumns} columns allowed.`);
            // Uncheck the checkbox
            const checkbox = this.container.querySelector(`.column-checkbox[value="${field}"]`);
            if (checkbox) checkbox.checked = false;
            return;
        }

        const column = this.columns.find(c => c.field === field);
        if (!column) return;

        const item = this.createSelectedColumnItem(field, column.label, selectedCount);
        this.selectedList.appendChild(item);
        this.updateOrder();

        // Update visual state
        const row = this.container.querySelector(`.column-row[data-field="${field}"]`);
        if (row) row.classList.add('selected');

        if (this.options.onSelect) {
            this.options.onSelect({ field, label: column.label });
        }
    }

    deselectColumn(field) {
        const item = this.selectedList.querySelector(`.selected-column[data-field="${field}"]`);
        if (item) item.remove();
        this.updateOrder();

        // Update visual state
        const row = this.container.querySelector(`.column-row[data-field="${field}"]`);
        if (row) row.classList.remove('selected');

        if (this.options.onDeselect) {
            this.options.onDeselect({ field });
        }
    }

    createSelectedColumnItem(field, label, index) {
        const item = document.createElement('div');
        item.className = 'selected-column d-flex gap-2 align-items-center mb-2 p-2 bg-light rounded';
        item.dataset.field = field;

        item.innerHTML = `
            <span class="drag-handle text-muted cursor-move" title="Drag to reorder">
                <i class="fas fa-grip-vertical"></i>
            </span>
            <span class="flex-grow-1">${label}</span>
            <input type="hidden" name="${this.options.namePrefix}[${index}]" value="${field}">
            <button type="button" class="btn btn-sm btn-outline-danger remove-column" title="Remove column">
                <i class="fas fa-times"></i>
            </button>
        `;

        // Remove button handler
        item.querySelector('.remove-column').addEventListener('click', () => {
            // Uncheck the checkbox
            const checkbox = this.container.querySelector(`.column-checkbox[value="${field}"]`);
            if (checkbox) checkbox.checked = false;
            this.deselectColumn(field);
        });

        return item;
    }

    updateOrder() {
        const items = this.selectedList.querySelectorAll('.selected-column');
        items.forEach((item, index) => {
            const input = item.querySelector('input[type="hidden"]');
            if (input) {
                input.name = `${this.options.namePrefix}[${index}]`;
            }
        });

        if (this.options.onChange) {
            this.options.onChange();
        }
    }

    // Get currently selected columns in order
    getSelectedColumns() {
        const columns = [];
        this.selectedList.querySelectorAll('.selected-column').forEach(item => {
            columns.push(item.dataset.field);
        });
        return columns;
    }

    // Select columns programmatically (for loading existing view)
    setSelectedColumns(fields) {
        // Clear current selection
        this.selectedList.innerHTML = '';
        this.container.querySelectorAll('.column-checkbox').forEach(cb => cb.checked = false);
        this.container.querySelectorAll('.column-row').forEach(r => r.classList.remove('selected'));

        // Select each field
        fields.forEach((field, index) => {
            const checkbox = this.container.querySelector(`.column-checkbox[value="${field}"]`);
            if (checkbox) {
                checkbox.checked = true;
                const column = this.columns.find(c => c.field === field);
                if (column) {
                    const item = this.createSelectedColumnItem(field, column.label, index);
                    this.selectedList.appendChild(item);
                    
                    const row = this.container.querySelector(`.column-row[data-field="${field}"]`);
                    if (row) row.classList.add('selected');
                }
            }
        });

        this.updateOrder();
    }
}

// Auto-initialize column selectors on page load
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-column-selector]').forEach(container => {
        new ColumnSelector(container);
    });
});

// Export for module use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ColumnSelector;
}


/**
 * Advanced Filter Builder Component
 * Provides dynamic AND/OR condition building for ticket view filters.
 */
class FilterBuilder {
    constructor(container, options = {}) {
        this.container = typeof container === 'string' 
            ? document.querySelector(container) 
            : container;
        
        if (!this.container) {
            console.error('FilterBuilder: Container not found');
            return;
        }

        this.options = {
            maxConditions: 20,
            namePrefix: 'Conditions.Filters',
            onAdd: null,
            onRemove: null,
            onChange: null,
            ...options
        };

        // Attribute definitions passed via data attribute
        this.attributes = JSON.parse(this.container.dataset.attributes || '[]');
        this.users = JSON.parse(this.container.dataset.users || '[]');
        this.teams = JSON.parse(this.container.dataset.teams || '[]');
        this.statuses = JSON.parse(this.container.dataset.statuses || '[]');
        this.priorities = JSON.parse(this.container.dataset.priorities || '[]');
        this.datePresets = JSON.parse(this.container.dataset.datePresets || '[]');

        this.init();
    }

    init() {
        this.conditionsList = this.container.querySelector('.filter-conditions');
        this.logicToggle = this.container.querySelector('[data-filter-logic]');
        this.addBtn = this.container.querySelector('[data-add-condition]');

        if (this.addBtn) {
            this.addBtn.addEventListener('click', () => this.addCondition());
        }

        if (this.logicToggle) {
            this.logicToggle.addEventListener('click', () => this.toggleLogic());
        }

        // Initialize existing conditions
        this.conditionsList?.querySelectorAll('.filter-condition').forEach((row, index) => {
            this.initConditionRow(row, index);
        });

        this.updateIndices();
    }

    toggleLogic() {
        const currentLogic = this.logicToggle.dataset.filterLogic;
        const newLogic = currentLogic === 'AND' ? 'OR' : 'AND';
        this.logicToggle.dataset.filterLogic = newLogic;
        this.logicToggle.textContent = newLogic;
        
        // Update hidden input
        const hiddenInput = this.container.querySelector('input[name="Conditions.Logic"]');
        if (hiddenInput) {
            hiddenInput.value = newLogic;
        }

        if (this.options.onChange) {
            this.options.onChange({ logic: newLogic });
        }
    }

    addCondition(values = {}) {
        const conditions = this.conditionsList.querySelectorAll('.filter-condition');
        if (conditions.length >= this.options.maxConditions) {
            alert(`Maximum ${this.options.maxConditions} conditions allowed.`);
            return;
        }

        const index = conditions.length;
        const row = this.createConditionRow(index, values);
        this.conditionsList.appendChild(row);
        this.initConditionRow(row, index);
        this.updateAddButtonState();

        if (this.options.onAdd) {
            this.options.onAdd({ index, row });
        }
    }

    createConditionRow(index, values = {}) {
        const row = document.createElement('div');
        row.className = 'filter-condition d-flex gap-2 align-items-start mb-2';
        row.dataset.index = index;

        const prefix = `${this.options.namePrefix}[${index}]`;

        row.innerHTML = `
            <select name="${prefix}.Field" class="form-select form-select-sm attribute-select" style="width: 200px;">
                <option value="">Select field...</option>
                ${this.attributes.map(a => `
                    <option value="${a.field}" 
                            data-type="${a.type}"
                            ${values.field === a.field ? 'selected' : ''}>
                        ${a.label}
                    </option>
                `).join('')}
            </select>
            
            <select name="${prefix}.Operator" class="form-select form-select-sm operator-select" style="width: 180px;">
                <option value="">Select operator...</option>
            </select>
            
            <div class="value-input-container flex-grow-1" style="min-width: 200px;">
                <input type="text" name="${prefix}.Value" class="form-control form-control-sm value-input" placeholder="Value..." value="${values.value || ''}">
            </div>
            
            <button type="button" class="btn btn-sm btn-outline-danger remove-condition" title="Remove condition">
                <i class="fas fa-times"></i>
            </button>
        `;

        return row;
    }

    initConditionRow(row, index) {
        const attributeSelect = row.querySelector('.attribute-select');
        const operatorSelect = row.querySelector('.operator-select');
        const valueContainer = row.querySelector('.value-input-container');
        const removeBtn = row.querySelector('.remove-condition');

        // Attribute change handler
        attributeSelect.addEventListener('change', () => {
            this.updateOperators(row, attributeSelect.value);
            this.updateValueInput(row, attributeSelect.value, operatorSelect.value);
        });

        // Operator change handler
        operatorSelect.addEventListener('change', () => {
            this.updateValueInput(row, attributeSelect.value, operatorSelect.value);
        });

        // Remove handler
        removeBtn.addEventListener('click', () => {
            this.removeCondition(row);
        });

        // If there's a pre-selected attribute, update operators
        if (attributeSelect.value) {
            this.updateOperators(row, attributeSelect.value);
        }
    }

    updateOperators(row, field) {
        const operatorSelect = row.querySelector('.operator-select');
        const attribute = this.attributes.find(a => a.field === field);
        
        if (!attribute || !attribute.operators) {
            operatorSelect.innerHTML = '<option value="">Select operator...</option>';
            return;
        }

        const currentValue = operatorSelect.value;
        operatorSelect.innerHTML = attribute.operators.map(op => `
            <option value="${op.value}" 
                    data-requires-value="${op.requiresValue}"
                    data-multiple="${op.allowsMultipleValues}"
                    ${currentValue === op.value ? 'selected' : ''}>
                ${op.label}
            </option>
        `).join('');
    }

    updateValueInput(row, field, operator) {
        const container = row.querySelector('.value-input-container');
        const attribute = this.attributes.find(a => a.field === field);
        const op = attribute?.operators?.find(o => o.value === operator);
        const prefix = `${this.options.namePrefix}[${row.dataset.index}]`;

        // Check if value is required
        if (!op?.requiresValue) {
            container.innerHTML = '<span class="text-muted small">No value needed</span>';
            return;
        }

        // Generate input based on attribute type
        let html = '';
        switch (attribute?.type) {
            case 'selection':
            case 'status':
                html = this.createStatusSelect(prefix, op.allowsMultipleValues);
                break;
            case 'priority':
                html = this.createPrioritySelect(prefix, op.allowsMultipleValues);
                break;
            case 'user':
                html = this.createUserSelect(prefix, op.allowsMultipleValues);
                break;
            case 'team':
                html = this.createTeamSelect(prefix, op.allowsMultipleValues);
                break;
            case 'date':
                html = this.createDateInput(prefix, operator);
                break;
            case 'boolean':
                html = '<span class="text-muted small">No value needed</span>';
                break;
            default:
                html = `<input type="text" name="${prefix}.Value" class="form-control form-control-sm value-input" placeholder="Enter value...">`;
        }

        container.innerHTML = html;

        // Initialize any special inputs
        if (attribute?.type === 'date') {
            this.initDateInput(container);
        }
    }

    createStatusSelect(prefix, multiple = false) {
        const name = multiple ? `${prefix}.Values` : `${prefix}.Value`;
        return `
            <select name="${name}" class="form-select form-select-sm" ${multiple ? 'multiple' : ''}>
                <optgroup label="Groups">
                    <option value="__OPEN__">Open (all non-closed)</option>
                    <option value="__CLOSED__">Closed (resolved/cancelled)</option>
                </optgroup>
                <optgroup label="Individual Statuses">
                    ${this.statuses.map(s => `<option value="${s.value}">${s.label}</option>`).join('')}
                </optgroup>
            </select>
        `;
    }

    createPrioritySelect(prefix, multiple = false) {
        const name = multiple ? `${prefix}.Values` : `${prefix}.Value`;
        return `
            <select name="${name}" class="form-select form-select-sm" ${multiple ? 'multiple' : ''}>
                ${this.priorities.map(p => `<option value="${p.value}">${p.label}</option>`).join('')}
            </select>
        `;
    }

    createUserSelect(prefix, multiple = false) {
        const name = multiple ? `${prefix}.Values` : `${prefix}.Value`;
        return `
            <select name="${name}" class="form-select form-select-sm" ${multiple ? 'multiple' : ''}>
                <option value="">Select user...</option>
                ${this.users.map(u => `
                    <option value="${u.id}">${u.name}${u.isDeactivated ? ' (deactivated)' : ''}</option>
                `).join('')}
            </select>
        `;
    }

    createTeamSelect(prefix, multiple = false) {
        const name = multiple ? `${prefix}.Values` : `${prefix}.Value`;
        return `
            <select name="${name}" class="form-select form-select-sm" ${multiple ? 'multiple' : ''}>
                <option value="">Select team...</option>
                ${this.teams.map(t => `<option value="${t.id}">${t.name}</option>`).join('')}
            </select>
        `;
    }

    createDateInput(prefix, operator) {
        const showPresets = ['is', 'is_within'].includes(operator);
        
        return `
            <div class="d-flex gap-2 align-items-center">
                ${showPresets ? `
                    <select name="${prefix}.RelativeDatePreset" class="form-select form-select-sm date-preset-select" style="width: 160px;">
                        <option value="">Choose...</option>
                        ${this.datePresets.map(p => `<option value="${p.value}">${p.label}</option>`).join('')}
                    </select>
                    <div class="relative-days-input d-none">
                        <input type="number" name="${prefix}.RelativeDateValue" class="form-control form-control-sm" style="width: 80px;" placeholder="Days">
                    </div>
                ` : ''}
                <input type="text" name="${prefix}.Value" class="form-control form-control-sm date-picker-input" placeholder="Pick date..." style="width: 140px;">
            </div>
        `;
    }

    initDateInput(container) {
        const presetSelect = container.querySelector('.date-preset-select');
        const daysInput = container.querySelector('.relative-days-input');
        const dateInput = container.querySelector('.date-picker-input');

        if (presetSelect) {
            presetSelect.addEventListener('change', () => {
                const value = presetSelect.value;
                if (value === 'days_ago' || value === 'days_from_now') {
                    daysInput?.classList.remove('d-none');
                    dateInput?.classList.add('d-none');
                } else if (value === 'exact_date') {
                    daysInput?.classList.add('d-none');
                    dateInput?.classList.remove('d-none');
                } else {
                    daysInput?.classList.add('d-none');
                    dateInput?.classList.add('d-none');
                }
            });
        }

        // Initialize flatpickr if available
        if (window.flatpickr && dateInput) {
            flatpickr(dateInput, {
                dateFormat: 'Y-m-d',
                allowInput: true
            });
        }
    }

    removeCondition(row) {
        row.remove();
        this.updateIndices();
        this.updateAddButtonState();

        if (this.options.onRemove) {
            this.options.onRemove();
        }
    }

    updateIndices() {
        const conditions = this.conditionsList.querySelectorAll('.filter-condition');
        conditions.forEach((row, index) => {
            row.dataset.index = index;
            row.querySelectorAll('[name]').forEach(input => {
                input.name = input.name.replace(/\[\d+\]/, `[${index}]`);
            });
        });
    }

    updateAddButtonState() {
        const count = this.conditionsList.querySelectorAll('.filter-condition').length;
        if (this.addBtn) {
            this.addBtn.disabled = count >= this.options.maxConditions;
        }
    }

    // Get current filter configuration
    getFilters() {
        const logic = this.logicToggle?.dataset.filterLogic || 'AND';
        const filters = [];

        this.conditionsList.querySelectorAll('.filter-condition').forEach(row => {
            const field = row.querySelector('.attribute-select')?.value;
            const operator = row.querySelector('.operator-select')?.value;
            const value = row.querySelector('.value-input, select[name*=".Value"]')?.value;

            if (field && operator) {
                filters.push({ field, operator, value });
            }
        });

        return { logic, filters };
    }
}

// Auto-initialize filter builders on page load
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-filter-builder]').forEach(container => {
        new FilterBuilder(container);
    });
});

// Export for module use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FilterBuilder;
}


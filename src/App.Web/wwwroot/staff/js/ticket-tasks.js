/**
 * ticket-tasks.js — Inline task management for the Ticket Detail view.
 * Handles: create, edit title, complete/reopen, delete, reorder,
 *          inline assignee picker (Team/Individual format), inline due date picker,
 *          dependency picker, and apply template.
 */
(function () {
    'use strict';

    const section = document.getElementById('tasks-section');
    if (!section) return;

    const tasksList = document.getElementById('tasks-list');
    const newTaskInput = document.getElementById('new-task-input');

    // Extract ticket ID from URL
    const ticketId = window.location.pathname.match(/\/tickets\/(\d+)/i)?.[1];
    if (!ticketId) return;

    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    async function postJson(handler, body) {
        const resp = await fetch(`?handler=${handler}&id=${ticketId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(body)
        });
        return resp.json();
    }

    function escapeHtml(str) {
        const d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    function closeAllPickers() {
        document.querySelectorAll('.task-picker').forEach(el => el.remove());
    }

    /** Load pre-rendered assignee options from the hidden container */
    function getAssigneeOptions() {
        const container = document.getElementById('task-assignee-options');
        if (!container) return [];
        return Array.from(container.querySelectorAll('span[data-value]')).map(el => ({
            value: el.dataset.value,
            text: el.dataset.text
        }));
    }

    // ──── Create Task ────
    if (newTaskInput) {
        newTaskInput.addEventListener('keydown', async (e) => {
            if (e.key !== 'Enter') return;
            e.preventDefault();
            const title = newTaskInput.value.trim();
            if (!title) return;

            newTaskInput.disabled = true;
            const data = await postJson('CreateTask', { title });
            newTaskInput.disabled = false;
            newTaskInput.value = '';

            if (data.success && data.result) {
                window.location.reload();
            }
        });
    }

    // ──── Complete / Reopen ────
    tasksList.addEventListener('click', async (e) => {
        const btn = e.target.closest('.task-complete-btn');
        if (!btn) return;

        const item = btn.closest('.task-item');
        const taskId = item.dataset.taskId;
        const action = btn.dataset.action;

        btn.disabled = true;
        btn.style.opacity = '0.5';

        const handler = action === 'complete' ? 'CompleteTask' : 'ReopenTask';
        const data = await postJson(handler, { taskId });

        if (data.success) {
            window.location.reload();
        } else {
            btn.disabled = false;
            btn.style.opacity = '';
            alert(data.error || 'Failed to update task.');
        }
    });

    // ──── Inline Title Editing ────
    tasksList.addEventListener('focusout', async (e) => {
        if (!e.target.matches('[data-field="title"]')) return;
        const item = e.target.closest('.task-item');
        const taskId = item.dataset.taskId;
        const newTitle = e.target.textContent.trim();

        if (!newTitle) {
            e.target.textContent = 'Untitled task';
        }
        await postJson('UpdateTask', { taskId, title: newTitle || 'Untitled task' });
    });

    tasksList.addEventListener('keydown', (e) => {
        if (e.target.matches('[data-field="title"]') && e.key === 'Enter') {
            e.preventDefault();
            e.target.blur();
        }
    });

    // ──── Delete ────
    tasksList.addEventListener('click', async (e) => {
        const btn = e.target.closest('.task-delete-btn');
        if (!btn) return;
        if (!confirm('Delete this task?')) return;

        const item = btn.closest('.task-item');
        const taskId = item.dataset.taskId;
        const data = await postJson('DeleteTask', { taskId });
        if (data.success) {
            window.location.reload();
        }
    });

    // ──── Drag & Drop Reorder ────
    if (typeof Sortable !== 'undefined') {
        new Sortable(tasksList, {
            handle: '.drag-handle',
            animation: 150,
            ghostClass: 'bg-light',
            onEnd: async function () {
                const items = tasksList.querySelectorAll('.task-item');
                const orderedIds = Array.from(items).map(el => el.dataset.taskId);
                await postJson('ReorderTasks', { orderedIds });
            }
        });
    }

    // ──── Assignee Picker (Team/Individual format) ────
    tasksList.addEventListener('click', async (e) => {
        const btn = e.target.closest('.task-assign-btn');
        if (!btn) return;
        e.stopPropagation();

        const item = btn.closest('.task-item');
        const taskId = item.dataset.taskId;
        closeAllPickers();

        const options = getAssigneeOptions();

        const picker = document.createElement('div');
        picker.className = 'task-picker';

        let html = '<div class="task-picker-header">Assign to</div>';
        html += '<input type="text" class="task-picker-search" placeholder="Search assignees..." autofocus />';
        html += '<div class="task-picker-divider"></div>';
        html += '<div class="task-picker-list">';

        options.forEach(opt => {
            const icon = opt.value === 'unassigned' ? 'bi-x-circle'
                : (opt.value.includes(':assignee:') ? 'bi-person-fill' : 'bi-people-fill');
            const cls = opt.value === 'unassigned' ? 'task-picker-option--danger' : '';
            html += `<div class="task-picker-option ${cls}" data-assign-value="${escapeHtml(opt.value)}">
                <i class="bi ${icon}"></i> ${escapeHtml(opt.text)}
            </div>`;
        });

        if (!options.length) {
            html += '<div class="text-muted small p-2 text-center">No assignees available</div>';
        }
        html += '</div>';
        picker.innerHTML = html;

        item.style.position = 'relative';
        item.appendChild(picker);

        const btnRect = btn.getBoundingClientRect();
        const itemRect = item.getBoundingClientRect();
        picker.style.top = (btnRect.bottom - itemRect.top + 4) + 'px';
        picker.style.right = '8px';

        // Search filtering
        const searchInput = picker.querySelector('.task-picker-search');
        const allOptions = picker.querySelectorAll('.task-picker-option');
        searchInput.addEventListener('input', () => {
            const q = searchInput.value.toLowerCase();
            allOptions.forEach(opt => {
                const text = opt.textContent.toLowerCase();
                opt.style.display = text.includes(q) ? '' : 'none';
            });
        });

        // Handle selection
        picker.querySelectorAll('.task-picker-option').forEach(opt => {
            opt.addEventListener('click', async () => {
                const val = opt.dataset.assignValue;
                picker.remove();

                const body = { taskId };
                if (val === 'unassigned') {
                    body.clearAssignee = true;
                } else if (val.startsWith('team:')) {
                    const parts = val.split(':');
                    body.owningTeamId = parts[1];
                    if (parts.length === 4 && parts[2] === 'assignee') {
                        body.assigneeId = parts[3];
                    } else {
                        // Team/Anyone — clear individual assignee
                        body.clearAssignee = true;
                    }
                }
                await postJson('UpdateTask', body);
                window.location.reload();
            });
        });

        searchInput.focus();
        registerPickerClose(picker);
    });

    // ──── Due Date Picker ────
    tasksList.addEventListener('click', (e) => {
        const btn = e.target.closest('.task-due-btn');
        if (!btn) return;
        e.stopPropagation();

        const item = btn.closest('.task-item');
        const taskId = item.dataset.taskId;
        closeAllPickers();

        const picker = document.createElement('div');
        picker.className = 'task-picker';
        picker.style.minWidth = '220px';
        picker.innerHTML = `
            <div class="task-picker-header">Due date</div>
            <input type="datetime-local" class="form-control form-control-sm mb-2" id="task-due-input" />
            <div class="d-flex gap-2">
                <button class="btn btn-sm btn-primary flex-grow-1" id="task-due-save">
                    <i class="bi bi-check2 me-1"></i>Set
                </button>
                <button class="btn btn-sm btn-outline-danger" id="task-due-clear" title="Clear due date">
                    <i class="bi bi-x-lg"></i>
                </button>
            </div>
        `;

        item.style.position = 'relative';
        item.appendChild(picker);

        const btnRect = btn.getBoundingClientRect();
        const itemRect = item.getBoundingClientRect();
        picker.style.top = (btnRect.bottom - itemRect.top + 4) + 'px';
        picker.style.right = '8px';

        const dueDateInput = picker.querySelector('#task-due-input');

        picker.querySelector('#task-due-save').addEventListener('click', async () => {
            const val = dueDateInput.value;
            if (!val) return;
            picker.remove();
            await postJson('UpdateTask', { taskId, dueAt: new Date(val).toISOString() });
            window.location.reload();
        });

        picker.querySelector('#task-due-clear').addEventListener('click', async () => {
            picker.remove();
            await postJson('UpdateTask', { taskId, clearDueAt: true });
            window.location.reload();
        });

        dueDateInput.focus();
        registerPickerClose(picker);
    });

    // ──── Dependency Picker ────
    tasksList.addEventListener('click', (e) => {
        const btn = e.target.closest('.task-dep-btn');
        if (!btn) return;
        e.stopPropagation();

        const item = btn.closest('.task-item');
        const taskId = item.dataset.taskId;
        closeAllPickers();

        const allItems = tasksList.querySelectorAll('.task-item');
        const options = [];
        allItems.forEach(el => {
            if (el.dataset.taskId !== taskId) {
                const title = el.querySelector('.task-title')?.textContent?.trim();
                options.push({ id: el.dataset.taskId, title: title || 'Untitled' });
            }
        });

        const picker = document.createElement('div');
        picker.className = 'task-picker';

        let html = '<div class="task-picker-header">Depends on</div>';
        html += `<div class="task-picker-option task-picker-option--danger" data-dep-id="">
            <i class="bi bi-x-circle"></i> None (remove dependency)
        </div>`;
        html += '<div class="task-picker-divider"></div>';
        html += '<div class="task-picker-list">';
        options.forEach(opt => {
            html += `<div class="task-picker-option" data-dep-id="${opt.id}">
                <i class="bi bi-diagram-2"></i> ${escapeHtml(opt.title)}
            </div>`;
        });
        if (!options.length) {
            html += '<div class="text-muted small p-2 text-center">No other tasks</div>';
        }
        html += '</div>';
        picker.innerHTML = html;

        item.style.position = 'relative';
        item.appendChild(picker);

        const btnRect = btn.getBoundingClientRect();
        const itemRect = item.getBoundingClientRect();
        picker.style.top = (btnRect.bottom - itemRect.top + 4) + 'px';
        picker.style.right = '8px';

        picker.querySelectorAll('.task-picker-option').forEach(opt => {
            opt.addEventListener('click', async () => {
                const depId = opt.dataset.depId;
                picker.remove();
                if (depId) {
                    await postJson('UpdateTask', { taskId, dependsOnTaskId: depId });
                } else {
                    await postJson('UpdateTask', { taskId, clearDependency: true });
                }
                window.location.reload();
            });
        });

        registerPickerClose(picker);
    });

    // ──── Apply Template ────
    document.querySelectorAll('.apply-template-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const templateId = this.dataset.templateId;
            const templateName = this.dataset.templateName;
            if (!confirm(`Apply template "${templateName}"? This will add tasks to this ticket.`)) return;

            const response = await fetch(`?handler=ApplyTaskTemplate&id=${ticketId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getToken() },
                body: JSON.stringify({ templateId })
            });
            const data = await response.json();
            if (data.success) {
                window.location.reload();
            } else {
                alert(data.error || 'Failed to apply template.');
            }
        });
    });

    // ──── Picker close helper ────
    function registerPickerClose(picker) {
        setTimeout(() => {
            const handler = (ev) => {
                if (!picker.contains(ev.target) && !ev.target.closest('.task-action-btn')) {
                    picker.remove();
                    document.removeEventListener('click', handler, true);
                }
            };
            document.addEventListener('click', handler, true);
        }, 10);
    }
})();

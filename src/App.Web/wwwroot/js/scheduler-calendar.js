/**
 * Scheduler Calendar Interactivity
 * Minimal vanilla JS for the scheduler calendar views.
 */
(function () {
    'use strict';

    // ---- Multi-Select Dropdown ----
    function initMultiSelects() {
        document.querySelectorAll('.scheduler-multi-select').forEach(function (container) {
            var toggle = container.querySelector('.multi-select-toggle');
            var dropdown = container.querySelector('.multi-select-dropdown');
            var hiddenInput = container.querySelector('input[type="hidden"]');
            var placeholder = container.querySelector('.multi-select-placeholder');
            var pillsContainer = container.querySelector('.multi-select-pills');

            if (!toggle || !dropdown) return;

            toggle.addEventListener('click', function (e) {
                e.stopPropagation();
                var isOpen = dropdown.classList.contains('open');
                closeAllDropdowns();
                if (!isOpen) {
                    dropdown.classList.add('open');
                }
            });

            dropdown.querySelectorAll('.multi-select-option').forEach(function (option) {
                option.addEventListener('click', function (e) {
                    e.stopPropagation();
                    var checkbox = option.querySelector('input[type="checkbox"]');
                    checkbox.checked = !checkbox.checked;
                    option.classList.toggle('selected', checkbox.checked);
                    updateMultiSelectValue(container);
                });
            });
        });

        document.addEventListener('click', function () {
            closeAllDropdowns();
        });
    }

    function closeAllDropdowns() {
        document.querySelectorAll('.multi-select-dropdown.open').forEach(function (d) {
            d.classList.remove('open');
        });
    }

    function updateMultiSelectValue(container) {
        var hiddenInput = container.querySelector('input[type="hidden"]');
        var pillsContainer = container.querySelector('.multi-select-pills');
        var placeholder = container.querySelector('.multi-select-placeholder');
        var options = container.querySelectorAll('.multi-select-option');
        var selected = [];

        options.forEach(function (opt) {
            var cb = opt.querySelector('input[type="checkbox"]');
            if (cb && cb.checked) {
                selected.push({
                    value: cb.value,
                    label: opt.querySelector('.option-label')?.textContent || cb.value
                });
            }
        });

        if (hiddenInput) {
            hiddenInput.value = selected.map(function (s) { return s.value; }).join(',');
        }

        if (pillsContainer) {
            pillsContainer.innerHTML = '';
            selected.forEach(function (s) {
                var pill = document.createElement('span');
                pill.className = 'multi-select-pill';
                pill.innerHTML = s.label + ' <span class="pill-remove" data-value="' + s.value + '">&times;</span>';
                pillsContainer.appendChild(pill);
            });

            pillsContainer.querySelectorAll('.pill-remove').forEach(function (btn) {
                btn.addEventListener('click', function (e) {
                    e.stopPropagation();
                    var val = btn.getAttribute('data-value');
                    var opt = container.querySelector('.multi-select-option input[value="' + val + '"]');
                    if (opt) {
                        opt.checked = false;
                        opt.closest('.multi-select-option').classList.remove('selected');
                    }
                    updateMultiSelectValue(container);
                });
            });
        }

        if (placeholder) {
            placeholder.style.display = selected.length > 0 ? 'none' : '';
        }
    }

    // ---- Appointment Tooltip ----
    var tooltipEl = null;

    function initTooltips() {
        tooltipEl = document.createElement('div');
        tooltipEl.className = 'scheduler-tooltip';
        document.body.appendChild(tooltipEl);

        document.querySelectorAll('.scheduler-appointment[data-tooltip]').forEach(function (appt) {
            appt.addEventListener('mouseenter', function (e) {
                showTooltip(e, appt.getAttribute('data-tooltip'));
            });
            appt.addEventListener('mousemove', function (e) {
                positionTooltip(e);
            });
            appt.addEventListener('mouseleave', function () {
                hideTooltip();
            });
        });
    }

    function showTooltip(e, html) {
        if (!tooltipEl) return;
        tooltipEl.innerHTML = html;
        tooltipEl.classList.add('visible');
        positionTooltip(e);
    }

    function positionTooltip(e) {
        if (!tooltipEl) return;
        var x = e.clientX + 12;
        var y = e.clientY + 12;
        var rect = tooltipEl.getBoundingClientRect();
        if (x + rect.width > window.innerWidth) {
            x = e.clientX - rect.width - 12;
        }
        if (y + rect.height > window.innerHeight) {
            y = e.clientY - rect.height - 12;
        }
        tooltipEl.style.left = x + 'px';
        tooltipEl.style.top = y + 'px';
    }

    function hideTooltip() {
        if (tooltipEl) {
            tooltipEl.classList.remove('visible');
        }
    }

    // ---- Current Time Line ----
    function initNowLine() {
        updateNowLine();
        setInterval(updateNowLine, 60000);
    }

    function updateNowLine() {
        document.querySelectorAll('.scheduler-now-line').forEach(function (line) {
            var gridWrapper = line.closest('.scheduler-grid-wrapper');
            if (!gridWrapper) return;

            var startHour = parseInt(gridWrapper.getAttribute('data-start-hour') || '8', 10);
            var endHour = parseInt(gridWrapper.getAttribute('data-end-hour') || '18', 10);
            var now = new Date();
            var currentHour = now.getHours() + now.getMinutes() / 60;

            if (currentHour < startHour || currentHour > endHour) {
                line.style.display = 'none';
                return;
            }

            var totalHours = endHour - startHour;
            var offset = (currentHour - startHour) / totalHours;
            var gridHeight = gridWrapper.scrollHeight;
            var headerHeight = parseInt(gridWrapper.getAttribute('data-header-height') || '52', 10);

            line.style.display = '';
            line.style.top = (headerHeight + offset * (gridHeight - headerHeight)) + 'px';
        });
    }

    // ---- Block-Out Time Modal ----
    function initBlockOutModal() {
        var form = document.getElementById('blockOutForm');
        if (!form) return;

        var allDayCheckbox = form.querySelector('#blockOutAllDay');
        var timeFields = form.querySelectorAll('.blockout-time-field');

        if (allDayCheckbox) {
            allDayCheckbox.addEventListener('change', function () {
                timeFields.forEach(function (field) {
                    if (allDayCheckbox.checked) {
                        field.setAttribute('type', 'date');
                    } else {
                        field.setAttribute('type', 'datetime-local');
                    }
                });
            });
        }

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var formData = new FormData(form);
            var action = form.getAttribute('action');
            var token = form.querySelector('input[name="__RequestVerificationToken"]');

            fetch(action, {
                method: 'POST',
                body: formData,
                headers: token ? { 'RequestVerificationToken': token.value } : {}
            }).then(function (response) {
                if (response.ok || response.redirected) {
                    window.location.reload();
                } else {
                    alert('Failed to save block-out time. Please try again.');
                }
            }).catch(function () {
                alert('An error occurred. Please try again.');
            });
        });
    }

    // ---- Block-Out Delete ----
    function initBlockOutActions() {
        document.querySelectorAll('.blockout-delete-btn').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                if (!confirm('Delete this block-out time?')) return;

                var form = btn.closest('form');
                if (form) form.submit();
            });
        });
    }

    // ---- Init ----
    document.addEventListener('DOMContentLoaded', function () {
        initMultiSelects();
        initTooltips();
        initNowLine();
        initBlockOutModal();
        initBlockOutActions();
    });
})();

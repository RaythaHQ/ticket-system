/**
 * Scheduler Create Appointment Form — Dynamic behavior.
 *
 * Depends on global variables set by the Razor page:
 *   schedulerTypeData  — array of { id, name, mode, defaultDuration, eligibleStaff[] }
 *   schedulerStaffData — array of { id, name }
 *   defaultDurationMinutes — org default duration
 */
(function () {
    "use strict";

    var typeSelect = document.getElementById("appointmentTypeSelect");
    var staffSelect = document.getElementById("staffSelect");
    var durationInput = document.getElementById("durationInput");
    var modeSelector = document.getElementById("modeSelector");
    var modeHidden = document.getElementById("modeHidden");
    var meetingLinkGroup = document.getElementById("meetingLinkGroup");
    var coverageZoneGroup = document.getElementById("coverageZoneGroup");
    var modeVirtualRadio = document.getElementById("modeVirtual");
    var modeInPersonRadio = document.getElementById("modeInPerson");

    if (!typeSelect || !staffSelect) return;

    /**
     * Find the type data object by ID.
     */
    function getTypeById(typeId) {
        for (var i = 0; i < schedulerTypeData.length; i++) {
            if (schedulerTypeData[i].id === typeId) {
                return schedulerTypeData[i];
            }
        }
        return null;
    }

    /**
     * Get the currently resolved mode (from radio buttons or hidden field).
     */
    function getResolvedMode() {
        if (modeSelector.style.display !== "none") {
            if (modeVirtualRadio.checked) return "virtual";
            if (modeInPersonRadio.checked) return "in_person";
            return "";
        }
        return modeHidden.value;
    }

    /**
     * Update meeting link and coverage zone visibility based on resolved mode.
     */
    function updateModeFields() {
        var mode = getResolvedMode();

        // Show meeting link for virtual mode
        if (mode === "virtual") {
            meetingLinkGroup.style.display = "";
        } else {
            meetingLinkGroup.style.display = "none";
        }

        // Show coverage zone override for in-person mode
        if (mode === "in_person") {
            coverageZoneGroup.style.display = "";
        } else {
            coverageZoneGroup.style.display = "none";
        }
    }

    /**
     * Filter the staff dropdown to only show eligible staff for the selected type.
     */
    function filterStaffByType(typeId) {
        var typeInfo = getTypeById(typeId);
        var eligibleIds = typeInfo ? typeInfo.eligibleStaff : [];
        var currentValue = staffSelect.value;
        var hasCurrentValue = false;

        // Remove all options except the placeholder
        while (staffSelect.options.length > 1) {
            staffSelect.remove(1);
        }

        // If no type selected, show all staff
        var staffToShow = schedulerStaffData;
        if (typeId && eligibleIds.length > 0) {
            staffToShow = [];
            for (var i = 0; i < schedulerStaffData.length; i++) {
                for (var j = 0; j < eligibleIds.length; j++) {
                    if (schedulerStaffData[i].id === eligibleIds[j]) {
                        staffToShow.push(schedulerStaffData[i]);
                        break;
                    }
                }
            }
        }

        // Rebuild options
        for (var k = 0; k < staffToShow.length; k++) {
            var opt = document.createElement("option");
            opt.value = staffToShow[k].id;
            opt.textContent = staffToShow[k].name;
            staffSelect.appendChild(opt);
            if (staffToShow[k].id === currentValue) {
                hasCurrentValue = true;
            }
        }

        // Restore selection if still valid, otherwise reset
        if (hasCurrentValue) {
            staffSelect.value = currentValue;
        } else {
            staffSelect.value = "";
        }

        // Trigger change event so meeting link hint updates
        staffSelect.dispatchEvent(new Event("change"));
    }

    /**
     * Handle appointment type selection change.
     */
    function onTypeChange() {
        var selectedTypeId = typeSelect.value;
        var typeInfo = getTypeById(selectedTypeId);

        if (!typeInfo) {
            // No type selected — hide mode selector, show all staff
            modeSelector.style.display = "none";
            modeHidden.value = "";
            meetingLinkGroup.style.display = "none";
            coverageZoneGroup.style.display = "none";
            filterStaffByType("");
            durationInput.value = defaultDurationMinutes;
            return;
        }

        // Update duration from type default
        if (typeInfo.defaultDuration) {
            durationInput.value = typeInfo.defaultDuration;
        } else {
            durationInput.value = defaultDurationMinutes;
        }

        // Handle mode display
        if (typeInfo.mode === "either") {
            // Show mode chooser radio buttons
            modeSelector.style.display = "";
            modeHidden.value = "";
            modeHidden.disabled = true;
            // Reset radio selection
            modeVirtualRadio.checked = false;
            modeInPersonRadio.checked = false;
        } else {
            // Fixed mode — hide chooser, set hidden field
            modeSelector.style.display = "none";
            modeHidden.value = typeInfo.mode;
            modeHidden.disabled = false;
            // Clear radio buttons
            modeVirtualRadio.checked = false;
            modeInPersonRadio.checked = false;
        }

        updateModeFields();
        filterStaffByType(selectedTypeId);
    }

    /**
     * Handle mode radio button change.
     */
    function onModeChange() {
        updateModeFields();
    }

    // Bind event listeners
    typeSelect.addEventListener("change", onTypeChange);
    if (modeVirtualRadio) modeVirtualRadio.addEventListener("change", onModeChange);
    if (modeInPersonRadio) modeInPersonRadio.addEventListener("change", onModeChange);

    // Initialize on page load (in case of validation failure re-render)
    if (typeSelect.value) {
        onTypeChange();
    }
})();

/**
 * User Groups - Create Page
 * Handles user group creation form
 */

import { bindDeveloperNameSync } from '/admin/js/shared/developer-name-sync.js';
import { ready } from '/admin/js/core/events.js';
import { $ } from '/admin/js/core/dom.js';

function init() {
  const labelInput = $('#Form_Label');
  const devNameInput = $('#Form_DeveloperName');
  
  if (labelInput && devNameInput) {
    bindDeveloperNameSync(labelInput, devNameInput, {
      onlyIfEmpty: true
    });
  }
}

ready(init);


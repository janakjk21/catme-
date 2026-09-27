const year = document.getElementById('year');
if (year) year.textContent = new Date().getFullYear();

const menuButton = document.querySelector('[data-menu-toggle]');
const navigation = document.querySelector('[data-nav]');

if (menuButton && navigation) {
  const setMenuOpen = (open) => {
    menuButton.setAttribute('aria-expanded', String(open));
    menuButton.setAttribute('aria-label', open ? 'Close navigation' : 'Open navigation');
    navigation.classList.toggle('is-open', open);
  };

  menuButton.addEventListener('click', () => {
    setMenuOpen(menuButton.getAttribute('aria-expanded') !== 'true');
  });

  navigation.addEventListener('click', (event) => {
    if (event.target.closest('a')) setMenuOpen(false);
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') setMenuOpen(false);
  });

  document.addEventListener('click', (event) => {
    if (!event.target.closest('.site-header')) setMenuOpen(false);
  });
}

const betaStatus = document.querySelector('[data-beta-status]');
const betaSignupForm = document.querySelector('[data-beta-signup]');
const betaRemovalPanel = document.querySelector('[data-beta-remove]');

function setFormFeedback(element, message, state = '') {
  if (!element) return;
  element.textContent = message;
  if (state) element.dataset.state = state;
  else delete element.dataset.state;
}

async function requestJson(url, body) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(body),
    credentials: 'same-origin',
  });
  const result = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(result.error || 'We could not complete that request. Please try again later.');
  return result;
}

if (betaStatus && betaSignupForm) {
  fetch('/api/beta-signups/status', { headers: { Accept: 'application/json' }, cache: 'no-store' })
    .then((response) => response.ok ? response.json() : { open: false })
    .then(({ open }) => {
      if (open) {
        betaStatus.textContent = 'Beta invites are not available yet, but you can ask to hear when the first small test opens.';
        betaSignupForm.hidden = false;
        if (betaRemovalPanel) betaRemovalPanel.hidden = false;
      } else {
        betaStatus.textContent = 'Beta email sign-ups are not open just yet. No address will be saved until the invite list is ready.';
      }
    })
    .catch(() => {
      betaStatus.textContent = 'We can’t check beta sign-ups right now. No email has been collected.';
    });

  betaSignupForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    const feedback = betaSignupForm.querySelector('[data-beta-feedback]');
    const values = new FormData(betaSignupForm);
    const button = betaSignupForm.querySelector('button[type="submit"]');
    button.disabled = true;
    setFormFeedback(feedback, 'Saving your request…');
    try {
      const result = await requestJson('/api/beta-signups', {
        email: values.get('email'),
        consent: values.get('consent') === 'yes',
        website: values.get('website'),
      });
      betaSignupForm.reset();
      setFormFeedback(feedback, result.message || 'Thanks — your request has been received.', 'success');
    } catch (error) {
      setFormFeedback(feedback, error.message, 'error');
    } finally {
      button.disabled = false;
    }
  });
}

const betaRemovalForm = document.querySelector('[data-beta-removal]');
if (betaRemovalForm) {
  betaRemovalForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    const feedback = betaRemovalForm.querySelector('[data-remove-feedback]');
    const button = betaRemovalForm.querySelector('button[type="submit"]');
    button.disabled = true;
    setFormFeedback(feedback, 'Removing the address…');
    try {
      const result = await requestJson('/api/beta-signups/remove', {
        email: new FormData(betaRemovalForm).get('email'),
      });
      betaRemovalForm.reset();
      setFormFeedback(feedback, result.message || 'If that address was on the list, it has been removed.', 'success');
    } catch (error) {
      setFormFeedback(feedback, error.message, 'error');
    } finally {
      button.disabled = false;
    }
  });
}

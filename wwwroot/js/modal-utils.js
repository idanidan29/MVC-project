// Reusable modal utilities - Include this file in views that need custom modals
// Usage: showAlert('Title', 'Message', 'success|error|warning|info')
// Usage: showConfirm('Title', 'Message', callback)

function showAlert(title, message, type = 'info') {
    let modal = document.getElementById('globalAlertModal');
    if (!modal) {
        modal = document.createElement('div');
        modal.id = 'globalAlertModal';
        modal.className = 'custom-alert-modal';
        modal.innerHTML = `
            <div class="custom-alert-content">
                <div class="custom-alert-header">
                    <div class="custom-alert-icon" id="globalAlertIcon"><i class="bi bi-info-circle"></i></div>
                    <h3 id="globalAlertTitle">Alert</h3>
                </div>
                <div class="custom-alert-body"><p id="globalAlertMessage">Message</p></div>
                <div class="custom-alert-actions">
                    <button type="button" class="btn-alert-ok" onclick="closeAlert()">OK</button>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
        
        if (!document.getElementById('globalAlertStyles')) {
            const style = document.createElement('style');
            style.id = 'globalAlertStyles';
            style.textContent = `
                .custom-alert-modal{position:fixed;inset:0;background:rgba(15,23,42,0.65);display:none;align-items:center;justify-content:center;z-index:10000;backdrop-filter:blur(8px)}
                .custom-alert-modal.show{display:flex}
                .custom-alert-content{background:white;border-radius:1rem;max-width:480px;width:90%;padding:2rem;box-shadow:0 25px 70px rgba(0,0,0,0.3);animation:modalFadeIn .25s ease-out}
                @keyframes modalFadeIn{from{opacity:0;transform:scale(0.9)}to{opacity:1;transform:scale(1)}}
                .custom-alert-header{text-align:center;margin-bottom:1.5rem}
                .custom-alert-icon{width:64px;height:64px;margin:0 auto 1rem;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:2rem}
                .custom-alert-header h3{font-size:1.5rem;color:#1e293b;margin:0;font-weight:700}
                .custom-alert-body{margin-bottom:1.5rem;color:#475569;line-height:1.6;text-align:center}
                .custom-alert-body p{margin:0}
                .custom-alert-actions{display:flex;justify-content:center}
                .btn-alert-ok{background:linear-gradient(135deg,#f59e0b 0%,#d97706 100%);color:white;border:none;padding:.875rem 2rem;font-size:1rem;font-weight:600;border-radius:.75rem;cursor:pointer;transition:all .3s ease;box-shadow:0 4px 15px rgba(245,158,11,0.3)}
                .btn-alert-ok:hover{transform:translateY(-2px);box-shadow:0 6px 20px rgba(245,158,11,0.4)}
                .btn-confirm-cancel{flex:1;padding:.875rem 1rem;font-size:1rem;font-weight:600;border-radius:.75rem;cursor:pointer;border:2px solid #e5e7eb;background:white;color:#6b7280}
                .btn-confirm-ok{flex:1;background:linear-gradient(135deg,#ef4444 0%,#dc2626 100%);color:white;border:none;padding:.875rem 1rem;font-size:1rem;font-weight:600;border-radius:.75rem;cursor:pointer;box-shadow:0 4px 15px rgba(239,68,68,0.3)}
            `;
            document.head.appendChild(style);
        }
    }
    
    document.getElementById('globalAlertTitle').textContent = title;
    document.getElementById('globalAlertMessage').textContent = message;
    const iconEl = document.getElementById('globalAlertIcon');
    
    if (type === 'error') {
        iconEl.innerHTML = '<i class="bi bi-x-circle"></i>';
        iconEl.style.background = 'linear-gradient(135deg, #fee2e2 0%, #fca5a5 100%)';
        iconEl.style.color = '#dc2626';
    } else if (type === 'success') {
        iconEl.innerHTML = '<i class="bi bi-check-circle"></i>';
        iconEl.style.background = 'linear-gradient(135deg, #d1fae5 0%, #6ee7b7 100%)';
        iconEl.style.color = '#059669';
    } else if (type === 'warning') {
        iconEl.innerHTML = '<i class="bi bi-exclamation-triangle"></i>';
        iconEl.style.background = 'linear-gradient(135deg, #fef3c7 0%, #fde047 100%)';
        iconEl.style.color = '#ca8a04';
    } else {
        iconEl.innerHTML = '<i class="bi bi-info-circle"></i>';
        iconEl.style.background = 'linear-gradient(135deg, #dbeafe 0%, #93c5fd 100%)';
        iconEl.style.color = '#2563eb';
    }
    
    modal.classList.add('show');
}

function closeAlert() {
    const modal = document.getElementById('globalAlertModal');
    if (modal) modal.classList.remove('show');
}

function showConfirm(title, message, onConfirm) {
    let modal = document.getElementById('globalConfirmModal');
    if (!modal) {
        modal = document.createElement('div');
        modal.id = 'globalConfirmModal';
        modal.className = 'custom-alert-modal';
        modal.innerHTML = `
            <div class="custom-alert-content">
                <div class="custom-alert-header">
                    <div class="custom-alert-icon" style="background:linear-gradient(135deg,#fef3c7 0%,#fde047 100%);color:#ca8a04">
                        <i class="bi bi-question-circle"></i>
                    </div>
                    <h3 id="globalConfirmTitle">Confirm</h3>
                </div>
                <div class="custom-alert-body"><p id="globalConfirmMessage">Message</p></div>
                <div class="custom-alert-actions" style="gap: 1rem;">
                    <button type="button" class="btn-confirm-cancel" onclick="closeConfirm(false)">Cancel</button>
                    <button type="button" class="btn-confirm-ok" onclick="closeConfirm(true)">Confirm</button>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }
    
    document.getElementById('globalConfirmTitle').textContent = title;
    document.getElementById('globalConfirmMessage').textContent = message;
    window._confirmCallback = onConfirm;
    modal.classList.add('show');
}

function closeConfirm(confirmed) {
    const modal = document.getElementById('globalConfirmModal');
    if (modal) modal.classList.remove('show');
    if (confirmed && window._confirmCallback) {
        window._confirmCallback();
    }
    window._confirmCallback = null;
}

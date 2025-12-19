// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    // Password toggle functionality
    const togglePassword = document.querySelector('#togglePassword');
    const passwordInput = document.querySelector('#passwordInput');
    
    if (togglePassword && passwordInput) {
        togglePassword.addEventListener('click', function () {
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);
            this.classList.toggle('fa-eye');
            this.classList.toggle('fa-eye-slash');
        });
        togglePassword.style.cursor = 'pointer';
    }

    // Confirm Password toggle functionality
    const toggleConfirmPassword = document.querySelector('#toggleConfirmPassword');
    const confirmPasswordInput = document.querySelector('#confirmPasswordInput');
    
    if (toggleConfirmPassword && confirmPasswordInput) {
        toggleConfirmPassword.addEventListener('click', function () {
            const type = confirmPasswordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            confirmPasswordInput.setAttribute('type', type);
            this.classList.toggle('fa-eye');
            this.classList.toggle('fa-eye-slash');
        });
        toggleConfirmPassword.style.cursor = 'pointer';
    }

    // Admin: View user details modal
    document.body.addEventListener('click', async function (e) {
        const btn = e.target.closest('.view-user-details');
        if (!btn) return;
        const email = btn.getAttribute('data-email');
        if (!email) return;
        try {
            const res = await fetch(`/Admin/UserDetails?email=${encodeURIComponent(email)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const html = await res.text();
            const container = document.getElementById('userDetailsContent');
            if (container) {
                container.innerHTML = html;
                const modalEl = document.getElementById('userDetailsModal');
                if (modalEl) {
                    const modal = new bootstrap.Modal(modalEl);
                    modal.show();
                }
            }
        } catch (err) {
            console.error('Failed to load user details', err);
            alert('Could not load user details. Please try again.');
        }
    });

    // Admin: Delete user
    document.body.addEventListener('click', async function (e) {
        const btn = e.target.closest('#btnDeleteUser');
        if (!btn) return;
        const email = btn.getAttribute('data-email');
        if (!email) return;
        if (!confirm(`Are you sure you want to delete the user "${email}"? This action cannot be undone.`)) return;
        try {
            const res = await fetch('/Admin/DeleteUser', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `email=${encodeURIComponent(email)}`
            });
            const data = await res.json();
            if (data.success) {
                alert('User deleted successfully');
                const modalEl = document.getElementById('userDetailsModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();
                location.reload();
            } else {
                alert('Error: ' + data.message);
            }
        } catch (err) {
            console.error('Failed to delete user', err);
            alert('Could not delete user. Please try again.');
        }
    });

    // Admin: Open edit user modal
    document.body.addEventListener('click', async function (e) {
        const btn = e.target.closest('#btnEditUser');
        if (!btn) return;
        const email = btn.getAttribute('data-email');
        if (!email) return;
        try {
            const res = await fetch(`/Admin/EditUser?email=${encodeURIComponent(email)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const html = await res.text();
            const container = document.getElementById('userDetailsContent');
            if (container) {
                container.innerHTML = html;
            }
        } catch (err) {
            console.error('Failed to load edit form', err);
            alert('Could not load edit form. Please try again.');
        }
    });

    // Admin: Save user changes
    document.body.addEventListener('click', async function (e) {
        const btn = e.target.closest('#btnSaveUser');
        if (!btn) return;
        const email = document.getElementById('userEmail')?.value;
        const firstName = document.getElementById('firstName')?.value;
        const lastName = document.getElementById('lastName')?.value;
        const isAdmin = document.getElementById('isAdmin')?.checked ?? false;
        if (!email) return;
        try {
            const res = await fetch('/Admin/UpdateUser', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `email=${encodeURIComponent(email)}&firstName=${encodeURIComponent(firstName)}&lastName=${encodeURIComponent(lastName)}&isAdmin=${isAdmin}`
            });
            const data = await res.json();
            if (data.success) {
                alert('User updated successfully');
                const modalEl = document.getElementById('userDetailsModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();
                location.reload();
            } else {
                alert('Error: ' + data.message);
            }
        } catch (err) {
            console.error('Failed to update user', err);
            alert('Could not update user. Please try again.');
        }
    });

});
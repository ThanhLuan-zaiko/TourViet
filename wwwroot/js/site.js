// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Sidebar Toggle Functionality
document.addEventListener('DOMContentLoaded', function() {
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const mainContent = document.getElementById('mainContent');

    // Toggle sidebar
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('active');
            sidebarOverlay.classList.toggle('active');
        });
    }

    // Close sidebar when clicking overlay
    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', function() {
            sidebar.classList.remove('active');
            sidebarOverlay.classList.remove('active');
        });
    }

    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(event) {
        if (window.innerWidth <= 768) {
            if (sidebar.classList.contains('active') && 
                !sidebar.contains(event.target) && 
                !sidebarToggle.contains(event.target)) {
                sidebar.classList.remove('active');
                sidebarOverlay.classList.remove('active');
            }
        }
    });

    // Search functionality
    const searchInput = document.querySelector('.search-input');
    const searchBtn = document.querySelector('.search-btn');
    
    if (searchBtn) {
        searchBtn.addEventListener('click', function() {
            const searchTerm = searchInput.value.trim();
            if (searchTerm) {
                // Implement search functionality here
                console.log('Searching for:', searchTerm);
                // You can redirect to a search page or filter results
            }
        });
    }

    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                const searchTerm = searchInput.value.trim();
                if (searchTerm) {
                    // Implement search functionality here
                    console.log('Searching for:', searchTerm);
                }
            }
        });
    }

    // Bootstrap form validation
    (function() {
        'use strict';
        const forms = document.querySelectorAll('.needs-validation');
        
        Array.from(forms).forEach(function(form) {
            form.addEventListener('submit', function(event) {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                }
                
                form.classList.add('was-validated');
            }, false);
        });
    })();

    // Real-time password confirmation validation
    const registerPassword = document.getElementById('Password');
    const registerConfirmPassword = document.getElementById('ConfirmPassword');
    
    if (registerPassword && registerConfirmPassword) {
        registerConfirmPassword.addEventListener('input', function() {
            if (registerPassword.value !== registerConfirmPassword.value) {
                registerConfirmPassword.setCustomValidity('Mật khẩu xác nhận không khớp!');
            } else {
                registerConfirmPassword.setCustomValidity('');
            }
        });
    }

    // Real-time password confirmation validation for change password
    const newPassword = document.getElementById('NewPassword');
    const confirmNewPassword = document.getElementById('ConfirmNewPassword');
    
    if (newPassword && confirmNewPassword) {
        confirmNewPassword.addEventListener('input', function() {
            if (newPassword.value !== confirmNewPassword.value) {
                confirmNewPassword.setCustomValidity('Mật khẩu xác nhận không khớp!');
            } else {
                confirmNewPassword.setCustomValidity('');
            }
        });
    }

    // Responsive sidebar behavior
    function handleResize() {
        const sidebar = document.getElementById('sidebar');
        const sidebarOverlay = document.getElementById('sidebarOverlay');
        
        if (window.innerWidth > 991.98) {
            // On desktop, sidebar can stay open
            // You can add logic here if needed
        } else {
            // On mobile/tablet, close sidebar when resizing
            if (sidebar && sidebar.classList.contains('active')) {
                sidebar.classList.remove('active');
                if (sidebarOverlay) {
                    sidebarOverlay.classList.remove('active');
                }
            }
        }
    }

    // Handle window resize
    let resizeTimer;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(handleResize, 250);
    });
});
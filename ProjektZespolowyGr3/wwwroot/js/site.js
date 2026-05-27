// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function () {
    if (!window.TomSelect) {
        return;
    }

    document.querySelectorAll('select[id^="tagSelect"]').forEach(function (el) {
        new TomSelect(el, {
            plugins: ['remove_button'],
            placeholder: 'Wybierz tagi...',
            maxOptions: 200,
            maxItems: 20
        });
    });
});

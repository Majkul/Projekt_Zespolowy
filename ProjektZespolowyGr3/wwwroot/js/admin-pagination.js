// Paginacja list w panelu admina po stronie przeglądarki.
// Cała lista jest wczytywana z bazy i renderowana w tabeli, a ten skrypt
// dzieli wiersze na strony o rozmiarze data-page-size i dorysowuje kontrolki na dole.
(function () {
    "use strict";

    function buildPager(table, container, totalPages, currentPage, onGoTo) {
        container.innerHTML = "";
        if (totalPages <= 1) {
            return;
        }

        var ul = document.createElement("ul");
        ul.className = "pagination m-0";

        function addItem(label, page, opts) {
            opts = opts || {};
            var li = document.createElement("li");
            li.className = "page-item" + (opts.active ? " active" : "") + (opts.disabled ? " disabled" : "");
            var a = document.createElement("a");
            a.className = "page-link";
            a.href = "#";
            a.innerHTML = label;
            if (!opts.disabled && !opts.active && page) {
                a.addEventListener("click", function (e) {
                    e.preventDefault();
                    onGoTo(page);
                });
            } else {
                a.addEventListener("click", function (e) { e.preventDefault(); });
            }
            li.appendChild(a);
            ul.appendChild(li);
        }

        // « poprzednia
        addItem("&laquo;", currentPage - 1, { disabled: currentPage === 1 });

        // Okno numerów stron z wielokropkami (max ~7 numerów)
        var windowSize = 5;
        var start = Math.max(1, currentPage - Math.floor(windowSize / 2));
        var end = Math.min(totalPages, start + windowSize - 1);
        start = Math.max(1, Math.min(start, end - windowSize + 1));

        if (start > 1) {
            addItem("1", 1, {});
            if (start > 2) {
                addItem("&hellip;", null, { disabled: true });
            }
        }
        for (var i = start; i <= end; i++) {
            addItem(String(i), i, { active: i === currentPage });
        }
        if (end < totalPages) {
            if (end < totalPages - 1) {
                addItem("&hellip;", null, { disabled: true });
            }
            addItem(String(totalPages), totalPages, {});
        }

        // » następna
        addItem("&raquo;", currentPage + 1, { disabled: currentPage === totalPages });

        container.appendChild(ul);
    }

    function paginateTable(table) {
        var pageSize = parseInt(table.getAttribute("data-page-size"), 10) || 25;
        var tbody = table.tBodies[0];
        if (!tbody) {
            return;
        }
        var rows = Array.prototype.filter.call(tbody.rows, function () { return true; });
        var totalPages = Math.ceil(rows.length / pageSize);

        // Kontener na kontrolki — dorysowany pod tabelą (w stopce karty).
        var responsive = table.closest(".table-responsive") || table;
        var container = document.createElement("div");
        container.className = "card-footer d-flex justify-content-center admin-pager";
        responsive.parentNode.insertBefore(container, responsive.nextSibling);

        var currentPage = 1;

        function render() {
            var startIdx = (currentPage - 1) * pageSize;
            var endIdx = startIdx + pageSize;
            rows.forEach(function (row, idx) {
                row.style.display = (idx >= startIdx && idx < endIdx) ? "" : "none";
            });
            buildPager(table, container, totalPages, currentPage, goTo);
        }

        function goTo(page) {
            currentPage = Math.max(1, Math.min(page, totalPages));
            render();
        }

        render();
    }

    document.addEventListener("DOMContentLoaded", function () {
        var tables = document.querySelectorAll('table[data-paginate="true"]');
        Array.prototype.forEach.call(tables, paginateTable);
    });
})();

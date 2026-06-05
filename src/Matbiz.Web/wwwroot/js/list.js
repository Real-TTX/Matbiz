// Einheitliches Filter + Paging für Stammdaten-Listen.
//
// Pattern:
//   <input data-filter-search="<tbody-id>" placeholder="Suchen…" />
//   <span data-filter-count="<tbody-id>"></span>
//
//   <tbody id="<tbody-id>" data-paginate data-page-size="25">
//     <tr data-filter-text="suchbarer Text…">…</tr>
//   </tbody>
//
//   <div data-pagination="<tbody-id>"></div>
//
// Optional zusätzliche Filter (Dropdowns):
//   <select data-filter-attr="role" data-filter-target="<tbody-id>">
//     <option value="">Alle</option><option value="Admin">Admin</option>
//   </select>
//   → matcht gegen data-filter-role="…" auf der Row.
//
window.MatbizList = (function () {
    var states = {};

    function register(tbodyId, pageSize) {
        var tbody = document.getElementById(tbodyId);
        if (!tbody) return;
        states[tbodyId] = {
            pageSize: pageSize || 25,
            currentPage: 1,
            filterQuery: '',
            attrFilters: {}
        };
        render(tbodyId);
    }

    function setFilter(tbodyId, query) {
        var s = states[tbodyId]; if (!s) return;
        s.filterQuery = (query || '').toLowerCase().trim();
        s.currentPage = 1;
        render(tbodyId);
    }

    function setAttrFilter(tbodyId, attr, value) {
        var s = states[tbodyId]; if (!s) return;
        if (value) s.attrFilters[attr] = value; else delete s.attrFilters[attr];
        s.currentPage = 1;
        render(tbodyId);
    }

    function setPage(tbodyId, page) {
        var s = states[tbodyId]; if (!s) return;
        s.currentPage = page;
        render(tbodyId);
    }

    function smartPages(current, total) {
        if (total <= 7) {
            var arr = []; for (var i = 1; i <= total; i++) arr.push(i); return arr;
        }
        var set = {};
        [1, total, current, current - 1, current + 1, current - 2, current + 2].forEach(function (p) {
            if (p >= 1 && p <= total) set[p] = true;
        });
        var pages = Object.keys(set).map(Number).sort(function (a, b) { return a - b; });
        var result = []; var prev = 0;
        pages.forEach(function (p) {
            if (p - prev > 1) result.push('...');
            result.push(p);
            prev = p;
        });
        return result;
    }

    function render(tbodyId) {
        var tbody = document.getElementById(tbodyId);
        var s = states[tbodyId];
        if (!tbody || !s) return;

        var rows = [].slice.call(tbody.querySelectorAll('[data-filter-text]'));
        var filtered = rows.filter(function (r) {
            var txt = (r.getAttribute('data-filter-text') || '').toLowerCase();
            if (s.filterQuery && !txt.includes(s.filterQuery)) return false;
            for (var k in s.attrFilters) {
                if (r.getAttribute('data-filter-' + k) !== s.attrFilters[k]) return false;
            }
            return true;
        });

        var totalPages = Math.max(1, Math.ceil(filtered.length / s.pageSize));
        if (s.currentPage > totalPages) s.currentPage = totalPages;
        if (s.currentPage < 1) s.currentPage = 1;

        var start = (s.currentPage - 1) * s.pageSize;
        var end = start + s.pageSize;
        rows.forEach(function (r) { r.style.display = 'none'; });
        filtered.slice(start, end).forEach(function (r) { r.style.display = ''; });

        // Counter
        var counter = document.querySelector('[data-filter-count="' + tbodyId + '"]');
        if (counter) counter.textContent = filtered.length;

        // Pagination
        var pgContainer = document.querySelector('[data-pagination="' + tbodyId + '"]');
        if (pgContainer) renderPagination(pgContainer, tbodyId, s.currentPage, totalPages, filtered.length);
    }

    function renderPagination(container, tbodyId, currentPage, totalPages, totalRows) {
        if (totalPages <= 1) { container.innerHTML = ''; return; }
        var s = states[tbodyId];
        var from = (currentPage - 1) * s.pageSize + 1;
        var to = Math.min(currentPage * s.pageSize, totalRows);

        var html = '<div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-2">';
        html += '<div class="text-muted small">' + from + '–' + to + ' von ' + totalRows + '</div>';
        html += '<nav><ul class="pagination pagination-sm mb-0">';
        html += '<li class="page-item ' + (currentPage === 1 ? 'disabled' : '') + '"><a class="page-link" href="javascript:void(0)" onclick="MatbizList.setPage(\'' + tbodyId + '\', ' + (currentPage - 1) + ')">‹</a></li>';
        smartPages(currentPage, totalPages).forEach(function (p) {
            if (p === '...') {
                html += '<li class="page-item disabled"><span class="page-link">…</span></li>';
            } else {
                html += '<li class="page-item ' + (p === currentPage ? 'active' : '') + '"><a class="page-link" href="javascript:void(0)" onclick="MatbizList.setPage(\'' + tbodyId + '\', ' + p + ')">' + p + '</a></li>';
            }
        });
        html += '<li class="page-item ' + (currentPage === totalPages ? 'disabled' : '') + '"><a class="page-link" href="javascript:void(0)" onclick="MatbizList.setPage(\'' + tbodyId + '\', ' + (currentPage + 1) + ')">›</a></li>';
        html += '</ul></nav></div>';
        container.innerHTML = html;
    }

    // Auto-Init
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-paginate]').forEach(function (tbody) {
            var size = parseInt(tbody.dataset.pageSize || '25', 10);
            register(tbody.id, size);
        });
    });

    // Hooks: Suchfeld + Attribut-Filter
    document.addEventListener('input', function (e) {
        var t = e.target;
        if (t.matches && t.matches('[data-filter-search]')) {
            setFilter(t.getAttribute('data-filter-search'), t.value);
        }
    });
    document.addEventListener('change', function (e) {
        var t = e.target;
        if (t.matches && t.matches('[data-filter-attr][data-filter-target]')) {
            setAttrFilter(t.getAttribute('data-filter-target'), t.getAttribute('data-filter-attr'), t.value);
        }
    });

    return { register: register, setFilter: setFilter, setAttrFilter: setAttrFilter, setPage: setPage };
})();

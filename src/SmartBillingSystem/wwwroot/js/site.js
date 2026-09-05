(function () {
  'use strict';

  const ScrollAnimator = {
    observer: null,

    init() {
      const elements = document.querySelectorAll('[data-animate]');
      if (elements.length === 0) return;

      this.observer = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              entry.target.classList.add('is-visible');
              this.observer.unobserve(entry.target);
            }
          });
        },
        { rootMargin: '0px 0px -30px 0px', threshold: 0.1 }
      );

      elements.forEach((el) => this.observer.observe(el));
    },

    observeNew(selector) {
      if (!this.observer) return;
      document.querySelectorAll(selector + ':not(.is-visible)').forEach((el) => {
        this.observer.observe(el);
      });
    }
  };

  const ProductTable = {
    tbody: null,
    rowIndex: 0,

    init() {
      this.tbody = document.getElementById('product-rows');
      const addBtn = document.getElementById('add-row-btn');
      if (!this.tbody || !addBtn) return;

      addBtn.addEventListener('click', () => this.addRow());

      this.tbody.addEventListener('click', (e) => {
        const removeBtn = e.target.closest('.btn-remove-row');
        if (removeBtn) this.removeRow(removeBtn.closest('tr'));

        const suggestBtn = e.target.closest('.btn-suggest');
        if (suggestBtn) this.suggestProduct(suggestBtn);
      });

      if (this.tbody.querySelectorAll('tr').length === 0) {
        this.addRow();
      }
    },

    addRow(data) {
      const idx = this.rowIndex;
      this.rowIndex++;
      const tr = document.createElement('tr');
      tr.setAttribute('data-animate', 'fade-up');
      tr.innerHTML = `
        <td>
          <input type="text" name="items[${idx}].ProductName"
                 class="product-name" placeholder="Product name"
                 value="${data?.name || ''}" />
        </td>
        <td>
          <input type="number" name="items[${idx}].UnitPrice"
                 class="product-price" min="0" step="0.01"
                 placeholder="0.00"
                 value="${data?.unitPrice || ''}" />
        </td>
        <td>
          <input type="number" name="items[${idx}].Quantity"
                 class="product-qty" min="1" step="1"
                 placeholder="1"
                 value="${data?.quantity || 1}" />
        </td>
        <td class="row-total">
          <span class="product-row-total text-numeric">₹0.00</span>
        </td>
        <td style="white-space:nowrap; text-align:right;">
          <button type="button" class="btn btn--small btn--secondary btn-suggest"
                  title="AI Suggest" aria-label="Get AI suggestions">
            AI
          </button>
          <button type="button" class="btn btn--ghost btn-remove-row"
                  title="Remove" aria-label="Remove row">&times;</button>
        </td>
      `;

      this.tbody.appendChild(tr);

      const qtyInput = tr.querySelector('.product-qty');
      const priceInput = tr.querySelector('.product-price');
      qtyInput.addEventListener('input', () => TotalCalculator.recalculate());
      priceInput.addEventListener('input', () => TotalCalculator.recalculate());

      ScrollAnimator.observeNew('tr[data-animate]');
      tr.querySelector('.product-name').focus();
    },

    removeRow(tr) {
      if (!tr) return;
      if (this.tbody.querySelectorAll('tr').length <= 1) {
        tr.querySelector('.product-name').value = '';
        tr.querySelector('.product-qty').value = 1;
        tr.querySelector('.product-price').value = '';
        tr.querySelector('.product-row-total').textContent = '₹0.00';
        TotalCalculator.recalculate();
        return;
      }
      tr.style.opacity = '0';
      tr.style.transition = 'opacity 100ms';
      setTimeout(() => {
        tr.remove();
        TotalCalculator.recalculate();
      }, 100);
    },

    suggestProduct(btn) {
      const tr = btn.closest('tr');
      const nameInput = tr.querySelector('.product-name');
      const productName = nameInput?.value?.trim();

      if (!productName) {
        nameInput?.focus();
        return;
      }

      btn.disabled = true;
      btn.textContent = '...';

      fetch('/Bill/GetRecommendation?productName=' + encodeURIComponent(productName))
        .then(r => r.json())
        .then(data => {
          const recs = data.recommendations || [];
          if (recs.length > 0) {
            this.showProductRecommendations(tr, recs);
          }
        })
        .catch(() => {})
        .finally(() => {
          btn.disabled = false;
          btn.textContent = 'AI';
        });
    },

    showProductRecommendations(tr, recommendations) {
      let recDiv = tr.querySelector('.inline-recommendations');
      if (!recDiv) {
        recDiv = document.createElement('div');
        recDiv.className = 'inline-recommendations';
        recDiv.setAttribute('colspan', '5');
        tr.appendChild(recDiv);
      }
      recDiv.innerHTML = `
        <div style="grid-column: 1/-1; padding: var(--space-3) 0; border-top: 1px solid var(--color-border-light);">
          <span class="text-label" style="display:block; margin-bottom: var(--space-2);">
            AI Recommends
          </span>
          <div style="display:flex; gap:var(--space-2); flex-wrap:wrap;">
            ${recommendations.map(rec => `
              <button type="button" class="btn btn--small btn--secondary btn-add-rec"
                      data-name="${rec}">${rec}</button>
            `).join('')}
          </div>
        </div>
      `;

      recDiv.querySelectorAll('.btn-add-rec').forEach(btn => {
        btn.addEventListener('click', () => {
          ProductTable.addRow({ name: btn.dataset.name, quantity: 1 });
          TotalCalculator.recalculate();
          recDiv.remove();
        });
      });
    }
  };

  const TotalCalculator = {
    init() {
      this.recalculate();
    },

    recalculate() {
      const rows = document.querySelectorAll('#product-rows tr');
      let grandTotal = 0;
      const taxRate = 0.18;

      rows.forEach((row) => {
        const qty = parseFloat(row.querySelector('.product-qty')?.value) || 0;
        const price = parseFloat(row.querySelector('.product-price')?.value) || 0;
        const rowTotal = qty * price;
        grandTotal += rowTotal;

        const totalSpan = row.querySelector('.product-row-total');
        if (totalSpan) {
          totalSpan.textContent = formatCurrency(rowTotal);
        }
      });

      const subtotal = grandTotal;
      const tax = subtotal * taxRate;
      const total = subtotal + tax;

      updateEl('subtotal-display', formatCurrency(subtotal));
      updateEl('tax-display', formatCurrency(tax));
      updateEl('grand-total-display', formatCurrency(total));
    }
  };

  function formatCurrency(amount) {
    return '₹' + amount.toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  function updateEl(id, text) {
    const el = document.getElementById(id);
    if (el) el.textContent = text;
  }

  document.addEventListener('DOMContentLoaded', () => {
    ScrollAnimator.init();
    ProductTable.init();
    TotalCalculator.init();
  });
})();

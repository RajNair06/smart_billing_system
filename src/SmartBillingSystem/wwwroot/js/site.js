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
      const suggestBtn = document.getElementById('suggest-btn');
      
      if (!this.tbody) return;

      addBtn?.addEventListener('click', () => this.addRow());
      suggestBtn?.addEventListener('click', () => this.suggestForAllProducts());

      this.tbody.addEventListener('click', (e) => {
        const removeBtn = e.target.closest('.btn-remove-row');
        if (removeBtn) this.removeRow(removeBtn.closest('tr'));
      });

      document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
          e.preventDefault();
          this.addRow();
        }
      });
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
      
      const firstInput = tr.querySelector('.product-name');
      if (firstInput && !data) {
        setTimeout(() => firstInput.focus(), 100);
      }

      return tr;
    },

    removeRow(tr) {
      if (!tr) return;
      tr.style.opacity = '0';
      tr.style.transition = 'opacity 100ms';
      setTimeout(() => {
        tr.remove();
        TotalCalculator.recalculate();
      }, 100);
    },

    async suggestForAllProducts() {
      const btn = document.getElementById('suggest-btn');
      const products = Array.from(document.querySelectorAll('.product-name'))
        .map(input => input.value.trim())
        .filter(name => name.length > 0);

      if (products.length === 0) {
        alert('Please add at least one product first');
        return;
      }

      btn.disabled = true;
      btn.textContent = 'Analyzing';
      btn.classList.add('btn--loading');

      try {
        const response = await fetch('/api/gemini/recommend', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ products })
        });

        const data = await response.json();
        this.displayRecommendations(data.recommendations || []);
      } catch (error) {
        console.error('Failed to get recommendations:', error);
      } finally {
        btn.disabled = false;
        btn.textContent = 'Suggest Products';
        btn.classList.remove('btn--loading');
      }
    },

    displayRecommendations(recommendations) {
      const section = document.getElementById('recommendations-section');
      const list = document.getElementById('recommendations-list');

      list.innerHTML = '';

      if (recommendations.length === 0) {
        section.style.display = 'none';
        return;
      }

      recommendations.forEach(product => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn--ghost';
        btn.textContent = `+ ${product}`;
        btn.onclick = () => {
          this.addRow({ name: product, quantity: 1 });
          TotalCalculator.recalculate();
        };
        list.appendChild(btn);
      });

      section.style.display = 'block';
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

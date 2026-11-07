// MiniShop admin: progressive enhancement only. Every feature here also works (more plainly) without JS.
(function () {
    'use strict';
    document.documentElement.classList.add('js');

    // ---------- Broken or missing images fall back to the generic placeholder ----------
    document.addEventListener('error', function (e) {
        var img = e.target;
        if (!(img instanceof HTMLImageElement)) return;
        var fallback = img.getAttribute('data-fallback');
        if (fallback && img.getAttribute('src') !== fallback) img.src = fallback;
    }, true);

    // ---------- Delete confirmation: one modal filled from the trigger ----------
    var deleteModal = document.getElementById('deleteModal');
    if (deleteModal) {
        deleteModal.addEventListener('show.bs.modal', function (e) {
            var trigger = e.relatedTarget;
            if (!trigger) return;
            deleteModal.querySelector('[data-delete-id-field]').value = trigger.getAttribute('data-delete-id');
            deleteModal.querySelector('[data-delete-label]').textContent = '« ' + trigger.getAttribute('data-delete-name') + ' »';
        });
        deleteModal.querySelector('[data-delete-form]').addEventListener('submit', function (e) {
            if (!e.target.querySelector('[data-delete-id-field]').value) { e.preventDefault(); return; }
            e.target.querySelector('[type=submit]').disabled = true;
        });
    }

    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // French defaults for jQuery Validate (the model's own messages still win where they exist).
    // jquery.validate loads after this file, so wait for the page's scripts to have run.
    document.addEventListener('DOMContentLoaded', function () {
        if (!(window.jQuery && window.jQuery.validator)) return;
        window.jQuery.extend(window.jQuery.validator.messages, {
            required: 'Ce champ est obligatoire.',
            email: 'Saisissez une adresse e-mail valide.',
            number: 'Saisissez un nombre.',
            digits: 'Saisissez un nombre entier.',
            min: 'Saisissez une valeur supérieure ou égale à {0}.',
            max: 'Saisissez une valeur inférieure ou égale à {0}.',
            maxlength: 'Saisissez au plus {0} caractères.',
            minlength: 'Saisissez au moins {0} caractères.',
            step: 'Saisissez un multiple de {0}.'
        });
    });

    // ---------- Toasts: auto-hide, paused while hovered, focused or the tab is hidden ----------
    function initToast(toast) {
        var remaining = 5000, started = 0, timer = null;
        function dismiss() {
            clearTimeout(timer);
            toast.classList.add('is-leaving');
            setTimeout(function () { toast.remove(); }, 200);
        }
        function start() { if (!toast.hasAttribute('data-autohide') || timer) return; started = Date.now(); timer = setTimeout(dismiss, remaining); }
        function pause() { if (!timer) return; clearTimeout(timer); timer = null; remaining -= Date.now() - started; }
        toast.querySelector('[data-toast-dismiss]').addEventListener('click', dismiss);
        toast.addEventListener('pointerenter', pause);
        toast.addEventListener('pointerleave', start);
        toast.addEventListener('focusin', pause);
        toast.addEventListener('focusout', start);
        document.addEventListener('visibilitychange', function () { document.hidden ? pause() : start(); });
        start();
    }
    document.querySelectorAll('.toast-ui').forEach(initToast);

    function showToast(message, ok) {
        var region = document.querySelector('[data-toast-region]');
        if (!region) return;
        var toast = document.createElement('div');
        toast.className = 'toast-ui';
        toast.setAttribute('role', ok ? 'status' : 'alert');
        if (ok) toast.setAttribute('data-autohide', '');
        var icon = document.createElement('i');
        icon.className = 'bi toast-icon ' + (ok ? 'bi-check-circle-fill toast-icon--ok' : 'bi-exclamation-circle-fill toast-icon--error');
        icon.setAttribute('aria-hidden', 'true');
        var text = document.createElement('p');
        text.className = 'toast-text';
        text.textContent = message;
        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'toast-close';
        close.setAttribute('data-toast-dismiss', '');
        close.setAttribute('aria-label', 'Fermer');
        close.innerHTML = '<i class="bi bi-x-lg" aria-hidden="true"></i>';
        toast.append(icon, text, close);
        // Keep at most three on screen.
        while (region.children.length >= 3) region.firstElementChild.remove();
        region.appendChild(toast);
        initToast(toast);
    }

    // ---------- Storefront: add to cart without leaving the page ----------
    function updateCartBadge(count) {
        var badge = document.querySelector('[data-cart-count]');
        var link = document.querySelector('[data-cart-link]');
        if (!badge || !link) return;
        badge.textContent = count;
        badge.hidden = count === 0;
        link.setAttribute('aria-label', 'Panier, ' + count + (count === 1 ? ' article' : ' articles'));
        if (!reduceMotion) {
            badge.classList.remove('is-bumped');
            void badge.offsetWidth; // restart the animation
            badge.classList.add('is-bumped');
        }
    }

    // The button confirms the add for a moment: check icon and « Ajouté », then back to normal.
    function markAdded(button) {
        var icon = button.querySelector('.bi');
        var label = button.querySelector('span');
        if (!label || button.classList.contains('is-added')) return;
        var text = label.textContent, iconClass = icon ? icon.className : '';
        button.classList.add('is-added');
        label.textContent = 'Ajouté';
        if (icon) icon.className = 'bi bi-check2';
        setTimeout(function () {
            button.classList.remove('is-added');
            label.textContent = text;
            if (icon) icon.className = iconClass;
        }, 1600);
    }

    document.querySelectorAll('[data-add-to-cart]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            // "Acheter maintenant" is a normal submit: the server adds the item and opens the payment page.
            if (e.submitter && e.submitter.hasAttribute('data-buy-now')) return;
            e.preventDefault();
            var button = e.submitter || form.querySelector('[type=submit]');
            if (button.disabled) return;
            button.disabled = true;
            fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Requested-With': 'fetch' }, credentials: 'same-origin' })
                .then(function (r) { if (!r.ok && r.status !== 400) throw new Error(r.status); return r.json(); }) // 400: a refused option, with its message
                .then(function (data) {
                    showToast(data.message, data.ok);
                    updateCartBadge(data.count);
                    if (data.ok) markAdded(button);
                })
                .catch(function () { form.submit(); }) // fall back to a normal post
                .finally(function () { button.disabled = false; });
        });
    });

    // ---------- Product page: colour and storage options ----------
    // A colour swaps the photo (short crossfade, none with reduced motion) and the « Couleur » label;
    // a storage tier updates the price. The form posts colorId and storageId; the server prices them.
    var pdpImage = document.querySelector('[data-pdp-image]');
    var colorName = document.querySelector('[data-color-name]');
    var pdpPrice = document.querySelector('[data-pdp-price]');
    var swapId = 0;
    document.querySelectorAll('.swatch-input').forEach(function (input) {
        input.addEventListener('change', function () {
            var name = input.getAttribute('data-name');
            if (colorName) colorName.textContent = name;
            if (!pdpImage) return;
            var src = input.getAttribute('data-image');
            if (pdpImage.getAttribute('src') === src) return;
            var id = ++swapId;
            var base = pdpImage.getAttribute('data-base-alt') || pdpImage.alt.split(',')[0];
            pdpImage.setAttribute('data-base-alt', base);
            pdpImage.classList.add('is-swapping');
            var next = new Image();
            next.src = src;
            var swap = function () {
                if (id !== swapId) return; // a newer choice won
                pdpImage.src = src;
                pdpImage.alt = base + ', ' + name;
                requestAnimationFrame(function () { pdpImage.classList.remove('is-swapping'); });
            };
            (next.decode ? next.decode() : Promise.resolve()).then(swap, swap);
        });
    });
    document.querySelectorAll('.chip-input').forEach(function (input) {
        input.addEventListener('change', function () {
            if (pdpPrice) pdpPrice.textContent = input.getAttribute('data-price');
        });
    });

    // ---------- Quantity steppers (product page and cart) ----------
    document.querySelectorAll('[data-stepper]').forEach(function (stepper) {
        var input = stepper.querySelector('input');
        var minus = stepper.querySelector('[data-step="-1"]');
        var plus = stepper.querySelector('[data-step="1"]');
        var min = parseInt(input.min, 10) || 1;
        var max = parseInt(input.max, 10) || 10;
        function clamp(v) { v = parseInt(v, 10); if (isNaN(v)) v = min; return Math.min(max, Math.max(min, v)); }
        function sync() { minus.disabled = clamp(input.value) <= min; plus.disabled = clamp(input.value) >= max; }
        stepper.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-step]');
            if (!btn) return;
            var next = clamp(clamp(input.value) + parseInt(btn.getAttribute('data-step'), 10));
            if (String(next) === input.value) return;
            input.value = next;
            sync();
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
        input.addEventListener('change', function () { input.value = clamp(input.value); sync(); });
        sync();
    });

    // Cart lines save their quantity on their own, shortly after the last change.
    document.querySelectorAll('[data-cart-qty]').forEach(function (form) {
        var timer = null;
        form.addEventListener('change', function () {
            clearTimeout(timer);
            timer = setTimeout(function () { form.requestSubmit ? form.requestSubmit() : form.submit(); }, 450);
        });
    });

    // ---------- Payment page: format the (never submitted) card fields ----------
    var cardNumber = document.querySelector('[data-card-number]');
    if (cardNumber) {
        cardNumber.addEventListener('input', function () {
            cardNumber.value = cardNumber.value.replace(/\D/g, '').slice(0, 16).replace(/(\d{4})(?=\d)/g, '$1 ');
        });
    }
    var cardExpiry = document.querySelector('[data-card-expiry]');
    if (cardExpiry) {
        cardExpiry.addEventListener('input', function () {
            var d = cardExpiry.value.replace(/\D/g, '').slice(0, 4);
            cardExpiry.value = d.length > 2 ? d.slice(0, 2) + '/' + d.slice(2) : d;
        });
    }
    var checkoutForm = document.querySelector('[data-checkout-form]');
    if (checkoutForm) {
        checkoutForm.addEventListener('submit', function () {
            var $ = window.jQuery;
            if ($ && $.fn.valid && !$(checkoutForm).valid()) return;
            checkoutForm.querySelector('[data-submit]').disabled = true;
        });
    }

    // ---------- Dashboard: live search, category filter, scroll-spy ----------
    var filters = document.querySelector('[data-filters]');
    if (filters) {
        var search = filters.querySelector('[data-search]');
        var category = filters.querySelector('[data-category-filter]');
        var serverQuery = (search.value || '').trim().toLocaleLowerCase('fr');
        var sections = Array.prototype.slice.call(document.querySelectorAll('[data-section]'));
        var noResults = document.querySelector('[data-no-results]');
        var filterStatus = document.querySelector('[data-filter-status]');
        var navTimer = null;

        category.addEventListener('change', function () { filters.submit(); });

        function normalize(s) { return s.toLocaleLowerCase('fr').normalize('NFD').replace(/[̀-ͯ]/g, ''); }

        function applySearch() {
            var raw = search.value.trim();
            var q = normalize(raw);
            var total = 0;

            sections.forEach(function (section) {
                var rows = section.querySelectorAll('[data-product]');
                var shown = 0;
                rows.forEach(function (row) {
                    var match = !q || normalize(row.getAttribute('data-search-text')).indexOf(q) !== -1;
                    row.hidden = !match;
                    if (match) shown++;
                });
                total += shown;
                section.hidden = q !== '' && shown === 0;
                var count = document.querySelector('[data-count-for="' + section.getAttribute('data-section') + '"]');
                if (count) count.textContent = shown;
            });

            if (noResults) noResults.hidden = !(q && total === 0);
            document.dispatchEvent(new Event('sections:changed'));
            if (filterStatus && raw.toLocaleLowerCase('fr') !== serverQuery) filterStatus.hidden = true;

            // Keep the URL shareable: ?q= follows what is typed.
            var url = new URL(window.location.href);
            if (raw) url.searchParams.set('q', raw); else url.searchParams.delete('q');
            history.replaceState(null, '', url);

            // The server already narrowed the list: a broader query needs fresh results.
            clearTimeout(navTimer);
            if (serverQuery && raw.toLocaleLowerCase('fr').indexOf(serverQuery) === -1) {
                navTimer = setTimeout(function () { filters.submit(); }, 400);
            }
        }

        search.addEventListener('input', applySearch);

    }

    // ---------- Sticky category tabs (admin and storefront): scroll-spy ----------
    // Scroll-spy: highlight the tab of the section in view, and keep that tab visible in the strip.
    var nav = document.querySelector('[data-catnav-list]');
    var links = nav ? nav.querySelectorAll('[data-target]') : [];
    var sections = Array.prototype.slice.call(document.querySelectorAll('[data-section]'));
    function setCurrent(slug) {
        links.forEach(function (a) {
            var on = a.getAttribute('data-target') === slug;
            if (on) {
                a.setAttribute('aria-current', 'true');
                var left = a.offsetLeft - nav.clientWidth / 2 + a.clientWidth / 2;
                nav.scrollTo({ left: left, behavior: reduceMotion ? 'auto' : 'smooth' });
            } else {
                a.removeAttribute('aria-current');
            }
        });
    }
    // The current section is the last visible one whose top has passed under the sticky bars.
    if (links.length) {
        var current = null, ticking = false;
        var update = function () {
            ticking = false;
            var line = parseFloat(getComputedStyle(document.documentElement).scrollPaddingTop) || 0;
            var active = null;
            sections.forEach(function (s) {
                if (!s.hidden && s.getBoundingClientRect().top - line <= 1) active = s;
            });
            var visibleSections = sections.filter(function (s) { return !s.hidden; });
            // At the very bottom a short last section can never reach the line: it is still the one being read.
            if (window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2) active = visibleSections[visibleSections.length - 1];
            if (!active) active = visibleSections[0];
            if (active && active.id !== current) { current = active.id; setCurrent(current); }
        };
        window.addEventListener('scroll', function () {
            if (!ticking) { ticking = true; requestAnimationFrame(update); }
        }, { passive: true });
        document.addEventListener('sections:changed', update);
        update();
    }

    // Many aisles: the strip scrolls sideways inside its bar; a fade marks each edge that hides tabs.
    if (nav) {
        var fades = function () {
            var hidden = nav.scrollWidth - nav.clientWidth;
            nav.classList.toggle('is-clipped-start', nav.scrollLeft > 2);
            nav.classList.toggle('is-clipped-end', nav.scrollLeft < hidden - 2);
        };
        nav.addEventListener('scroll', fades, { passive: true });
        window.addEventListener('resize', fades);
        fades();
    }

    // ---------- Category form: live address preview and character counter ----------
    var catForm = document.querySelector('[data-category-form]');
    if (catForm) {
        var catName = catForm.querySelector('#Name');
        var slugPreview = catForm.querySelector('[data-slug-preview]');
        var catCounter = catForm.querySelector('[data-counter-for="Name"]');
        var catMax = parseInt(catName.getAttribute('data-max-chars'), 10);
        // Ids already on the pages (CategorySlug.Reserved on the server) and the other categories' slugs.
        var taken = ['main', 'meilleures-ventes', 'hero-title', 'hero-product-name', 'q', 'category']
            .concat((slugPreview.getAttribute('data-taken') || '').split(' ').filter(Boolean));
        var savedName = (catName.getAttribute('data-saved-name') || '').trim();
        var savedSlug = slugPreview.getAttribute('data-current-slug');
        // Same rules as CategorySlug.From.
        var slugify = function (name) {
            var s = name.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
                .replace(/œ/g, 'oe').replace(/æ/g, 'ae').replace(/ß/g, 'ss')
                .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
            if (s.length > 40) s = s.slice(0, 40).replace(/-+$/, '');
            return s || 'rayon';
        };
        // Visible characters: an emoji counts as one, as on the server.
        var visibleLength = function (s) {
            return window.Intl && Intl.Segmenter ? Array.from(new Intl.Segmenter('fr').segment(s)).length : Array.from(s).length;
        };
        var refresh = function () {
            var name = catName.value.replace(/\s+/g, ' ').trim();
            var slug = savedSlug;
            if (!savedSlug || name !== savedName) {
                var root = slugify(name);
                slug = root;
                for (var n = 2; taken.indexOf(slug) !== -1; n++) slug = root + '-' + n;
            }
            slugPreview.textContent = name ? '/#' + slug : '/#…';
            var len = visibleLength(name);
            catCounter.textContent = len + ' / ' + catMax;
            catCounter.classList.toggle('is-over', len > catMax);
        };
        catName.addEventListener('input', refresh);
        refresh();
        catForm.addEventListener('submit', function () { catForm.querySelector('[data-submit]').disabled = true; });
    }

    // Back from a reorder or an edit: the row that changed takes the focus, so the keyboard stays there.
    var targetRow = window.location.hash && document.querySelector('[data-category-row]:target');
    if (targetRow) targetRow.focus({ preventScroll: true });

    // ---------- Product form: image drop zones, colour / storage rows, client checks, counter ----------
    var allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];

    function checkImage(input) {
        var f = input.files && input.files[0];
        if (!f) return '';
        var maxBytes = parseInt(input.getAttribute('data-max-bytes'), 10);
        if (allowedTypes.indexOf(f.type) === -1 && !/\.(jpe?g|png|webp)$/i.test(f.name)) return 'Formats acceptés : JPG, PNG ou WebP.';
        if (f.size > maxBytes) return "L'image dépasse " + Math.round(maxBytes / 1048576) + ' Mo.';
        return '';
    }

    // One dashed square: its own file input, preview, « Changer » / « Retirer » and drag and drop.
    function initDropzone(zone) {
        if (zone.hasAttribute('data-ready')) return;
        zone.setAttribute('data-ready', '');
        var fileInput = zone.querySelector('input[type=file]');
        var emptyState = zone.querySelector('[data-dropzone-empty]');
        var previewBox = zone.querySelector('[data-dropzone-preview]');
        var preview = zone.querySelector('[data-preview-img]');
        var changeLabel = zone.querySelector('[data-change-label]');
        var removeBtn = zone.querySelector('[data-remove-image]');
        var imageError = document.getElementById(fileInput.getAttribute('data-error'));
        var currentSrc = preview.getAttribute('data-current') || '';
        var objectUrl = null;

        // src: what the square shows ('' = the empty drop zone); isNew: a file picked here, not saved yet.
        function showImage(src, isNew) {
            if (src) {
                if (preview.getAttribute('src') !== src) {
                    preview.classList.add('is-loading');
                    preview.onload = function () { preview.classList.remove('is-loading'); };
                    preview.src = src;
                }
            } else {
                preview.removeAttribute('src');
            }
            emptyState.hidden = !!src;
            previewBox.hidden = !src;
            zone.classList.toggle('has-image', !!src);
            removeBtn.hidden = !isNew;
            changeLabel.textContent = isNew ? 'Changer' : "Changer l'image";
        }

        fileInput.addEventListener('change', function () {
            if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
            var f = fileInput.files && fileInput.files[0];
            var err = checkImage(fileInput);
            if (imageError) imageError.textContent = err;
            fileInput.setAttribute('aria-invalid', err ? 'true' : 'false');
            if (!f || err) {
                if (err) fileInput.value = '';
                showImage(currentSrc, false);
                return;
            }
            objectUrl = URL.createObjectURL(f);
            showImage(objectUrl, true);
        });

        removeBtn.addEventListener('click', function () {
            if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
            fileInput.value = '';
            if (imageError) imageError.textContent = '';
            fileInput.setAttribute('aria-invalid', 'false');
            showImage(currentSrc, false);
            fileInput.focus();
        });

        // Drag and drop: the dropped file goes into the real input, so the form posts it as usual.
        var depth = 0;
        zone.addEventListener('dragenter', function (e) { e.preventDefault(); depth++; zone.classList.add('is-dragover'); });
        zone.addEventListener('dragover', function (e) { e.preventDefault(); e.dataTransfer.dropEffect = 'copy'; });
        zone.addEventListener('dragleave', function () { if (--depth <= 0) { depth = 0; zone.classList.remove('is-dragover'); } });
        zone.addEventListener('drop', function (e) {
            e.preventDefault();
            depth = 0;
            zone.classList.remove('is-dragover');
            var files = e.dataTransfer && e.dataTransfer.files;
            if (!files || !files.length) return;
            try {
                var dt = new DataTransfer();
                dt.items.add(files[0]);
                fileInput.files = dt.files;
            } catch (err) {
                fileInput.files = files;
            }
            fileInput.dispatchEvent(new Event('change', { bubbles: true }));
        });
    }

    // « Couleurs » and « Stockage »: rows are cloned from a <template>; each gets a fresh key used in
    // Colors.Index / Colors[key].*, so removing or moving rows never breaks the model binding.
    var rowSeq = 0;
    function initRepeater(block) {
        var rows = block.querySelector('[data-rows]');
        var template = block.querySelector('[data-row-template]');
        var add = block.querySelector('[data-add-row]');
        var max = parseInt(block.getAttribute('data-max'), 10);
        function refresh() {
            var count = rows.querySelectorAll('[data-row]').length;
            add.disabled = count >= max;
            rows.querySelectorAll('[data-row]').forEach(function (row, i, all) {
                var up = row.querySelector('[data-move="-1"]');
                var down = row.querySelector('[data-move="1"]');
                if (up) up.disabled = i === 0;
                if (down) down.disabled = i === all.length - 1;
            });
        }
        add.addEventListener('click', function () {
            if (rows.querySelectorAll('[data-row]').length >= max) return;
            var key = 'n' + (++rowSeq);
            var html = template.innerHTML.replace(/__k__/g, key);
            var holder = document.createElement('div');
            holder.innerHTML = html.trim();
            var row = holder.firstElementChild;
            rows.appendChild(row);
            row.querySelectorAll('[data-dropzone]').forEach(initDropzone);
            var first = row.querySelector('input:not([type=hidden]):not([type=file])');
            if (first) first.focus();
            refresh();
        });
        rows.addEventListener('click', function (e) {
            var btn = e.target.closest('button');
            if (!btn) return;
            var row = btn.closest('[data-row]');
            if (btn.hasAttribute('data-remove-row')) {
                var next = row.nextElementSibling || row.previousElementSibling;
                row.remove();
                refresh();
                (next ? next.querySelector('[data-remove-row]') : add).focus();
            } else if (btn.hasAttribute('data-move')) {
                var dir = btn.getAttribute('data-move');
                if (dir === '-1' && row.previousElementSibling) rows.insertBefore(row, row.previousElementSibling);
                if (dir === '1' && row.nextElementSibling) rows.insertBefore(row.nextElementSibling, row);
                refresh();
                btn.focus();
            }
        });
        refresh();
    }

    var form = document.querySelector('[data-product-form]');
    if (form) {
        form.querySelectorAll('[data-dropzone]').forEach(initDropzone);
        form.querySelectorAll('[data-repeater]').forEach(initRepeater);
        var fileInput = form.querySelector('#imageFile');

        var desc = form.querySelector('#Description');
        var counter = form.querySelector('[data-counter-for="Description"]');
        if (desc && counter) {
            var max = parseInt(desc.getAttribute('maxlength'), 10);
            var update = function () {
                counter.textContent = desc.value.length + ' / ' + max;
                counter.classList.toggle('is-over', desc.value.length >= max);
            };
            desc.addEventListener('input', update);
            update();
        }

        form.addEventListener('submit', function (e) {
            var bad = [].find.call(form.querySelectorAll('input[type=file]'), function (i) { return checkImage(i); });
            if (bad) { e.preventDefault(); bad.focus(); return; }
            var $ = window.jQuery;
            if ($ && $.fn.valid && !$(form).valid()) return; // jQuery Validate shows the errors and focuses the first one
            form.querySelector('[data-submit]').disabled = true;
        });
    }
})();

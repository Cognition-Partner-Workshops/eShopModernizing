// Client-side equivalent of the ASP.NET Web Forms RequiredFieldValidator / RangeValidator
// controls (Display="Dynamic") used by the legacy Create and Edit pages.
(function () {
    'use strict';

    var currency = /^\s*([-\+])?(((\d+),)*)(\d+)(\.(\d{1,2}))?\s*$/;
    var integer = /^\s*([-\+])?(\d+)\s*$/;

    function isValid(validator) {
        var input = document.getElementById(validator.getAttribute('data-wf-for'));
        if (!input) {
            return true;
        }
        var value = input.value.replace(/^\s+|\s+$/g, '');
        var kind = validator.getAttribute('data-wf-validate');
        if (kind === 'required') {
            return value !== '';
        }
        if (value === '') {
            return true;
        }
        var number;
        if (kind === 'currency') {
            if (!currency.test(value)) {
                return false;
            }
            number = parseFloat(value.replace(/,/g, ''));
        } else {
            if (!integer.test(value)) {
                return false;
            }
            number = parseInt(value, 10);
        }
        return number >= parseFloat(validator.getAttribute('data-wf-min')) &&
            number <= parseFloat(validator.getAttribute('data-wf-max'));
    }

    function validate(validator) {
        var ok = isValid(validator);
        validator.style.display = ok ? 'none' : 'inline';
        return ok;
    }

    document.addEventListener('DOMContentLoaded', function () {
        var validators = Array.prototype.slice.call(document.querySelectorAll('[data-wf-validate]'));
        validators.forEach(function (validator) {
            var input = document.getElementById(validator.getAttribute('data-wf-for'));
            if (input) {
                input.addEventListener('change', function () {
                    validators.filter(function (v) { return v.getAttribute('data-wf-for') === input.id; }).forEach(validate);
                });
            }
        });
        var form = document.getElementById('ctl01');
        if (form) {
            form.addEventListener('submit', function (e) {
                var allValid = validators.map(validate).every(function (v) { return v; });
                if (!allValid) {
                    e.preventDefault();
                }
            });
        }
    });
}());

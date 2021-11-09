$(document).ready(function() {

    $('input[name="daterange"]').daterangepicker({ startDate: moment(), endDate: moment().add(1, 'month')});

    $('input[name="daterange"]').on('apply.daterangepicker', function(ev, picker) {

        $("#start-date-input").val(picker.startDate.format('YYYY-MM-DD'));
        $("#end-date-input").val(picker.endDate.format('YYYY-MM-DD'));
    });
});
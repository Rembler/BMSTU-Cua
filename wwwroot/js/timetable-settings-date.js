$(document).ready(function(){

    function toDate(dateStr) {
        var parts = dateStr.split(".")
        return new Date(parts[2], parts[1] - 1, parts[0])
    }

    var datesLine = $("#timetable-dates-info p").text();
    var oldEndDate = datesLine.slice(datesLine.lastIndexOf(" "))

    $("#datepicker").datepicker({
        
        dateFormat: "dd-mm-yy",
        minDate: toDate(oldEndDate),

        onSelect: function() {

            $("#continue-extend-timetable").prop("disabled", false);
            var oldLink = $("#appointment-settings-link").attr("href");
            var newLink;
            if (oldLink.indexOf("?") == -1) {
                newLink = oldLink;
            } else
            {
                newLink = oldLink.slice(0, oldLink.lastIndexOf("?"))
            }
            newLink = newLink + "?newEndDate=" + moment($(this).datepicker('getDate')).format("DD-MM-YYYY");
            $("#appointment-settings-link").attr("href", newLink);
        }
    });
});
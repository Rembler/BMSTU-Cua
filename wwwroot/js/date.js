$(document).ready(function(){

    var url = window.location.href;
    var timetableId = url.substring(url.lastIndexOf('/') + 1);

    var dates;

    function toDate(dateStr) {
        var parts = dateStr.split("-")
        return new Date(parts[2], parts[1] - 1, parts[0])
    }

    function disableDates(date) {
        var string = $.datepicker.formatDate('dd-mm-yy', date);
        return [dates.indexOf(string) != -1];
    }

    $.post("/Timetable/GetAvailableDates", { id: timetableId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                dates = data;
            }
        })

    $("#datepicker").datepicker({
        
        dateFormat: "dd-mm-yy",
        minDate: toDate($("#start-date").text()),
        maxDate: toDate($("#end-date").text()),
        beforeShowDay: disableDates,

        onSelect: function() {

            var list = $("#available-time").empty();
            var date = moment($(this).datepicker('getDate')).format("DD-MM-YYYY HH:mm");
            $("#make-an-appointment").prop('disabled', false);

            $.post("/Timetable/GetAvailableTime", { id: timetableId, date: date })
                .done(function(data) {

                    if (data == null) {
                        console.log("Status: FAIL");
                    } else {
                        data.sort().forEach(item => {
                            
                            var option = $("<option></option>").text(item);
                            list.append(option);
                        });
                    }
                })
        }
    });

    $("#make-an-appointment").click(function() {

        var time = $("#available-time").val();
        var hours = parseInt(time.substring(0, time.indexOf(":")));
        var minutes = parseInt(time.substring(time.indexOf(":") + 1, time.indexOf(" ")));
        var date = moment($("#datepicker")
            .datepicker('getDate'))
            .add(hours, "hours")
            .add(minutes, "minutes")
            .format("DD-MM-YYYY HH:mm");

        $.post("/Timetable/AddUser", { id: timetableId, startAt: date })
            .done(function(data) {
                
                window.location.replace(data.redirectUrl);
            });
    });
});
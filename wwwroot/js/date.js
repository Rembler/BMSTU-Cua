$(document).ready(function(){

    var timetableId;
    var clickedItem;

    $(".show-appointment-modal").click(function() {

        clickedItem = $(this).closest(".room-for-join-div");
        timetableId = clickedItem.find(".hidden-p").text();

        var dates;

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
    })

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
            .done(function() {
                
                $("#appointmentModal").modal("toggle");

                clickedItem.find(".show-appointment-modal").hide();
                clickedItem.find(".cancel-appointment").show();

                clickedItem.find(".available-appointments").hide();
                clickedItem.find(".own-appointment").text("Your appointment: " + date).show();

                // connection.invoke("JoinGroup", "");
            });
    });
});
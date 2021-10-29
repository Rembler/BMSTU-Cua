// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(".join-room-btn").click(function() {

    var room = $(this).closest(".room-for-join-div");
    var roomId = room.find(".hidden-p").text();
    $("#got-id").text(roomId);
    $("#got-id-for-request").text(roomId);
});

$("#addRoom").click(function() {

    var roomId = $("#got-id").text();
    $.post("/Room/AddUser", { id: roomId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                $("#roomJoinModal").modal("toggle");
                var toDel = $(`#roomId-${roomId}`).closest(".room-for-join-div");
                toDel.remove();
            }
        })
});

$("#sendRequest").click(function() {

    var roomId = $("#got-id-for-request").text();
    var comment = $("#comment-input").val();
    $.post("/Room/AddRequest", { id: roomId, comment: comment })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                $("#privateRoomJoinModal").modal("toggle");
                var toDeactivate = $(`#roomId-${roomId}`).closest(".room-for-join-div").find(".join-room-btn");
                toDeactivate.prop("disabled", true);
            }
        })
});

$(".acceptRequest").click(function() {

    var currentDiv = $(this).closest(".my-request-div")
    var userId = currentDiv.find(".request-id").text();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);
    $.post("/Room/AddUser", { id: roomId, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                currentDiv.remove();
            }
        })
});

$(".declineRequest").click(function() {

    var currentDiv = $(this).closest(".my-request-div")
    var userId = currentDiv.find(".request-id").text();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);
    $.post("/Room/Decline", { id: roomId, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                currentDiv.remove();
            }
        })
});

$("#addQueue").click(function() {

    var queueId = $("#got-id").text();
    // console.log(roomId + " " + queueId)
    $.post("/Queue/AddUser", { id: queueId})
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                $("#exampleModalCenter").modal("toggle");

                var queueDiv = $(`#queueId-${queueId}`).closest(".room-for-join-div");

                var toInc = queueDiv.find(".num-of-participants");
                
                toInc.text(function(i, oldVal) {
                    return parseInt(oldVal, 10) + 1;
                });

                var btn = queueDiv.find(".join-queue-btn").prop("disabled", true);
            }
        })
});


$("#name").click(function() {

    var direction = parseInt($("#direction").text());
    if ($(this).attr("class") == "dropdown-item clicked") {
        direction = -direction;
    }
    if ($(this).attr("class") == "dropdown-item") {
        $("#created-joined").attr("class", "dropdown-item");
        $(this).attr("class", "dropdown-item clicked");
    }

    var $rooms = $(".room-holder"),
        $roomsList = $rooms.children();

    $roomsList.sort(function(a, b) {
        var an = $(a).find(".room-name").text();
            bn = $(b).find(".room-name").text();
        
        if (an > bn) {
            return direction;
        }

        if (an < bn) {
            return -direction;
        }

        return 0;
    });

    $roomsList.detach().appendTo($rooms);
    $("#direction").text(direction)
});

$("#created-joined").click(function() {

    var direction = parseInt($("#direction").text());
    if ($(this).attr("class") == "dropdown-item clicked") {
        direction = -direction;
    }
    if ($(this).attr("class") == "dropdown-item") {
        $("#name").attr("class", "dropdown-item");
        $(this).attr("class", "dropdown-item clicked");
    }

    var $rooms = $(".room-holder"),
        $roomsList = $rooms.children();

    $roomsList.sort(function(a, b) {
        var an = $(a).find(".admin-name").text();
            bn = $(b).find(".admin-name").text();
        
        if (an == "Admin: me" && bn != an) {
            return direction;
        }

        if (bn == "Admin: me" && an != bn) {
            return -direction;
        }

        return 0;
    });

    $roomsList.detach().appendTo($rooms);
    $("#direction").text(direction)
});

$(".my-input").keyup(function() {

    var searchFor = $(this).val();
    var $rooms = $(".room-holder"),
        $roomsList = $rooms.children();
    
    $roomsList.each(function() {
        var name = $(this).find(".room-name").text();
        if (name.includes(searchFor)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

$("#room-cri").keyup(function() {

    var searchFor = $(this).val();
    var $rooms = $("#room-for-join-holder"),
        $roomsList = $rooms.children();
    
    $roomsList.each(function() {
        var name = $(this).find(".room-name").text();
        if (name.includes(searchFor)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

$("#admin-cri").keyup(function() {

    var searchFor = $(this).val();
    var $rooms = $("#room-for-join-holder"),
        $roomsList = $rooms.children();
    
    $roomsList.each(function() {
        var name = $(this).find(".admin-name").text();
        if (name.includes(searchFor)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

$("#company-cri").keyup(function() {

    var searchFor = $(this).val();
    var $rooms = $("#room-for-join-holder"),
        $roomsList = $rooms.children();
    
    $roomsList.each(function() {
        var name = $(this).find(".company-name").text();
        if (name.includes(searchFor)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

$("#private-checkbox").click(function() {

    var status = $(this).is(":checked");
    var $rooms = $("#room-for-join-holder"),
        $roomsList = $rooms.children();

    $roomsList.each(function() {
        var picName = $(this).find("ion-icon").attr("name");
        if (picName == "lock-open-outline" && status) {
            $(this).hide();
        } else {
            $(this).show();
        }
    });
});

$(".show-participants-btn").click(function() {

    var room = $(this).closest(".room-for-join-div");
    var roomId = room.find(".hidden-p").text();

    $.post("/Queue/GetParticipants", { id: roomId })
        .done(function(data) {

            var list = $("#participants-list");
            list.empty();


            if (data.length == 0) {
                list.removeClass("separate-div")
            }

            for (let index = 0; index < data.length; index++) {
                const element = data[index];
                var newEl = $(`<div class=\"content-div my-participant-div\"><p>${element}</p></div>`);
                list.attr("class", "separate-div separate-div-no-col");
                list.append(newEl);
            }
        });
});

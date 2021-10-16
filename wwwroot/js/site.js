// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function addUser(id) {
    
    $.post("/Room/AddUser", { id: id })
        .done(function (data) {
            
            if (data != null) {
                var result_str = "<p>"+data+"</p>";
                console.log(data);

                // $(`#users-list-${id}`).html(result_str);
                $(`#users-list-${id}`).append(result_str);
                console.log(data);
            }
        });
}

$(".join-room-btn").click(function() {

    var room = $(this).closest(".room-for-join-div");
    var roomId = room.find(".hidden-p").text();
    $("#got-id").text(roomId);
});

$("#addRoom").click(function() {

    var roomId = $("#got-id").text();
    $.post("/Room/AddUser", { id: roomId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                $("#exampleModalCenter").modal("toggle");
                var toDel = $(`#roomId-${roomId}`).closest(".room-for-join-div");
                toDel.remove();
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

$(".find-room-input").keyup(function() {

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
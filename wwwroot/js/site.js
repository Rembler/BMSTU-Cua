// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//  кнопка на странице доступных комнат - перекидывает айди комнаты из скрытого текста на карточке
//  в скрытый текст на модальном окне
$(".join-room-btn").click(function() {

    var room = $(this).closest(".room-for-join-div");
    var roomId = room.find(".hidden-p").text();
    $("#got-id").text(roomId);
    $("#got-id-for-request").text(roomId);
});

//  кнопка добавить комнату - отправляет пост запрос на сервер который устанавливает связь между 
//  данной комнатой и текущим пользователем
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

//  кнопка отправить запрос - появляется вместо кнопки добавить комнату если комната приватная -
//  отправляет пост запрос на сервер который создает или обновляет запись в таблице запросов
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

//  кнопка на странице управления для одобрения запроса - отправляет пост запрос на сервер который 
//  устанавливает связь между данной комнатой и пользователем оставившем заявку и обновляет
//  статус запроса в таблице запросов
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

//  другая кнопка на странице управления - отправляет пост запрос на сервер который только
//  обновляет статус запроса в таблице запросов
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

//  кнопка на странице очередей комнаты - отправляет пост запрос на сервер который устанавливает
//  связь между текущим пользователем и данной очередью
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

//  вариант в контекстном меню на главной странице который позволяет отсортировать комнаты
//  по их названию
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

//  вариант в контекстном меню на главной странице который позволяет отсортировать комнаты
//  по критерию "создал - присоединился"
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

//  инпут на главной странице реализующий поиск комнат по имени
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

//  инпут на странице доступных комнат реализующий поиск комнат по имени
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

//  инпут на странице доступных комнат реализующий поиск комнат по имени админа
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

//  инпут на странице доступных комнат реализующий поиск комнат по названию компании
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

//  чекбокс на странице доступных комнат отображающий только приватные комнаты
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

//  кнопка на карточке очереди - отправляет пост запрост на сервер который возвращает
//  полный список участников этой очереди
$(".show-participants-btn").click(function() {

    var room = $(this).closest(".room-for-join-div");
    var queueId = room.find(".hidden-p").text();

    $.post("/Queue/GetParticipants", { id: queueId })
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

//  кнопка смены статуса очереди - отправляет пост запрос на сервер который меняет
//  статус очереди на противоположный
$(".change-active-status").click(function() {

    var clickedBtn = $(this);
    var queueId = $(this).closest(".room-for-join-div").find(".hidden-p").text();

    $.post("/Queue/ChangeActiveStatus", { id: queueId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                if (clickedBtn.text() == "Start") {
                    clickedBtn.text("Stop");
                } else {
                    clickedBtn.text("Start");
                }
            }
        })
});

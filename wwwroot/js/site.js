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

    $.post("/Queue/Join", { id: queueId})
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

                queueDiv.find(".join-queue-btn").hide();
                queueDiv.find(".leave-queue-btn").show();
            }
        })
});

$(".leave-queue-btn").click(function() {

    var queueId = $("#got-id").text();

    $.post("/Queue/RemoveUser", { id: queueId })
    .done(function(data) {

        if (data == null) {
            console.log("Status: FAIL");
        } else {
            console.log("Status: OK");

            var queueDiv = $(`#queueId-${queueId}`).closest(".room-for-join-div");

            var toDec = queueDiv.find(".num-of-participants");
            
            toDec.text(function(i, oldVal) {
                return parseInt(oldVal, 10) - 1;
            });

            queueDiv.find(".join-queue-btn").show();
            queueDiv.find(".leave-queue-btn").hide();
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
$(".search-input").keyup(function() {

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

    $.post("/Queue/GetUsers", { id: queueId })
        .done(function(data) {

            var list = $("#participants-list");
            list.empty();

            if (data == 0) {
                list.removeClass("separate-div")
            }

            for (let index = 0; index < data.length; index++) {
                const element = data[index];
                var newEl = $(`<div class=\"content-div my-participant-div\"><p>${element.name}</p></div>`);
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

$(".change-description").click(function() {

    $(".my-info-div").hide();
    $(".my-control-input-holder").show();
    $(this).hide();
    $(".save-changes").show();
});

$(".save-changes").click(function() {

    var newName = $("#room-name-input").val();
    var newCompany = $("#company-name-input").val();
    var newAbout = $("#about-input").val();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Room/Update", { id: roomId, newName: newName, newCompany: newCompany, newAbout: newAbout })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                $("#name-info > p").text(newName);
                $("#company-info > p").text(newCompany);
                $("#about-info > p").text(newAbout);
                $(".my-info-div").show();

                $("#room-name-input").val(newName);
                $("#company-name-input").val(newCompany);
                $("#about-input").val(newAbout);
                $(".my-control-input-holder").hide();

                $(".save-changes").hide();
                $(".change-description").show();
            }
        })
});

$(".delete-user").click(function() {

    var clickedUser = $(this).closest(".my-participant-div");
    var userId = clickedUser.find(".user-id").text();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Room/DeleteUser", { id: roomId, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                clickedUser.remove();
            }
        })
});

$(".delete-queue").click(function() {

    var clickedQueue = $(this).closest(".my-participant-div");
    var queueId = clickedQueue.find(".queue-id").text();

    $.post("/Queue/Delete", { id: queueId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                clickedQueue.remove();
            }
        })
});

$(".queue-control").click(function() {

    var queueId = $(this).closest(".my-participant-div").find(".queue-id").text();
    $("#queue-id").text(queueId);

    $.post("/Queue/GetUsers", { id: queueId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: OK");

                var target = $("#my-queue-members");
                target.empty();

                if (data == 0) {
                    var infoDiv = $("<div></div>").addClass("content-div my-queue-member-holder");
                    var infoP = $("<p></p>").text("No users are in queue");

                    infoDiv.append(infoP);
                    target.append(infoDiv);
                    $("#remove-first-user").prop("disabled", true);
                } else {
                    data.sort((a, b) => (a.place > b.place ? 1 : -1));
                    data.forEach(element => {
                        var infoDiv = $("<div></div>").addClass("content-div my-queue-member-holder");
                        var infoP = $("<p></p>").text(element.name);
                        var hiddenP = $("<p></p>").addClass("queue-user-id").text(element.userId).hide();

                        infoDiv.append(infoP);
                        infoDiv.append(hiddenP);
                        target.append(infoDiv);
                    });
                }
            }
        })
});

$("#remove-first-user").click(function() {

    var toRemove = $("#my-queue-members").children(".my-queue-member-holder").eq(0);
    var queueId = $("#queue-id").text();
    var userId = toRemove.find(".queue-user-id").text();
    
    $.post("/Queue/RemoveUser", { id: queueId, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: OK");

                toRemove.remove();

                if ($("#my-queue-members").children().length == 0) {
                    var infoDiv = $("<div></div>").addClass("content-div my-queue-member-holder");
                    var infoP = $("<p></p>").text("No users are in queue");

                    infoDiv.append(infoP);
                    $("#my-queue-members").append(infoDiv);
                    $("#remove-first-user").prop("disabled", true);
                }
                
                var counter = $(`#user-count-${queueId}`);
                var userCount = parseInt(counter.text().substring(1, counter.text().length - 1)) - 1;
                counter.text("(" + userCount + ")");
            }
        })
});

$("#update-info").click(function() {

    if($(this).text() == "Update information")
    {
        $(".info-holder").hide();
        $(".general-input").show();
        $("#update-info").text("Save changes");
    } else {
        newName = $("#new-name").val();
        newSurname = $("#new-surname").val();
        newCompany = $("#new-company").val();

        if (newName == "" || newSurname == "" || newCompany == "") {
            alert("Some fields are empty");
        } else {
            $.post("/Account/UpdateInfo", { name: newName, surname: newSurname, company: newCompany })
                .done(function(data) {

                    if (data == null) {
                        console.log("Status: FAIL");
                    } else {
                        console.log("Status: OK");

                        $("#p-name").text(newName);
                        $("#p-surname").text(newSurname);
                        $("#p-company").text(newCompany);

                        $(".info-holder").show();
                        $(".general-input").hide();
                        $("#update-info").text("Update information");
                    }
                })
        }
    }
});

$("#update-email").click(function() {

    if($(this).text() == "Update email") {
        $(".email-holder").hide();
        $(".email-input").show();
        $("#update-email").text("Confirm email");
    } else {
        var email = $("#new-email").val();
        const re = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;

        if (re.test(email.toLowerCase())) {
            $.post("/Account/UpdateEmail", { email: email })
                .done(function(data) {

                    if (data == null) {
                        console.log("Status: FAIL");
                        alert("This email can't be chosen, try another one");
                    } else {
                        console.log("Status: OK");
                        alert("Email changed successfully. Now check your email box to confirm it");

                        $("#p-email").text(email);
                    
                        $(".email-holder").show();
                        $(".email-input").hide();
                        $("#update-email").text("Update email");
                    }
                })
        } else {
            alert("Incorrect email");
        }
    }
});

$("#update-password").click(function() {

    if ($(this).text() == "Update password") {
        $(".password-div").show();
        $(this).text("Save password");
    } else {
        var passwd = $("#new-password").val();
        var passwdConfirmation = $("#new-password-confirmation").val();

        if (passwd.length < 6) {
            alert("Password must contains at least 6 symbols");
        } else {
            if (passwd != passwdConfirmation) {
                alert("Passwords are not the same");
            } else {
                $.post("/Account/UpdatePassword", { password: passwd })
                    .done(function(data) {

                        if (data == null) {
                            console.log("Status: FAIL");
                            alert("Something goes wrong");
                        } else {
                            console.log("Status: OK");

                            $("#new-password").val("");
                            $("#new-password-confirmation").val("");
                            $(".password-div").hide();
                            $("#update-password").text("Update password");

                            alert("Password successfully changed");
                        }
                    })
            }
        }
    }
});

$(".change-moderator-status").click(function() {

    var clickedLink = $(this);
    var newLink = $(this).closest(".my-participant-div").find(".my-hidden-a");
    var userId = $(this).closest(".my-participant-div").find(".user-id").text();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Room/ChangeModeratorStatus", { id: roomId, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                clickedLink.addClass("my-hidden-a").hide();
                newLink.removeClass("my-hidden-a").show();
            }
        })
});
// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//  кнопка на странице доступных комнат - перекидывает айди комнаты из скрытого текста на карточке
//  в скрытый текст на модальном окне
// $(".join-room-btn").click(function() {

//     var room = $(this).closest(".room-for-join-div");
//     var roomId = room.find(".hidden-p").text();
//     console.log(roomId);
//     $("#got-id").text(roomId);
//     $("#got-id-for-request").text(roomId);
// });

$("#items-holder").on("click", ".id-giver", function() {

    var item = $(this).closest(".room-for-join-div");
    var itemId = item.find(".hidden-p").text();
    console.log(itemId);
    $("#got-id").text(itemId);
    $("#got-id-for-request").text(itemId);
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

$(".user-accept-request").click(function() {
    
    var toDel = $(this).closest(".request-div");
    var roomId = toDel.find(".room-id").text();

    $.post("/Room/AddUser", { id: roomId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                toDel.remove();
            }
        })
});

$(".user-decline-request").click(function() {

    var toDel = $(this).closest(".request-div");
    var roomId = toDel.find(".room-id").text();

    $.post("/Account/Decline", { roomId: roomId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                toDel.remove();
            }
        })
})

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
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Queue/Join", { id: queueId, roomId: roomId })
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
    var $rooms = $("#items-holder"),
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
    var $rooms = $("#items-holder"),
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
    var $rooms = $("#items-holder"),
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
    var $rooms = $("#items-holder"),
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
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Queue/GetUsers", { id: queueId, roomId: roomId })
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

//  кнопка скрывающая параграфы с информацией о комнате и показывающая инпуты
$(".change-description").click(function() {

    $(".my-info-div").hide();
    $(".my-control-input-holder").show();
    $(this).hide();
    $(".save-changes").show();
});

$(".save-queue-changes").click(function() {

    var newName = $("#queue-name-input").val();
    var newLimit = $("#limit-input").val();
    var newStartAt = $("#start-at-input").val();
    var url = window.location.href;
    var queueId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Queue/Update", { id: queueId, newName: newName, newLimit: newLimit, newStartAt: newStartAt })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                $("#queue-name-info > p").text(newName);
                $("#limit-info > p").text("Limit: " + newLimit);
                $("#start-at-info > p").text(moment(newStartAt).format('DD.MM.YYYY HH:mm'));
                $(".my-info-div").show();

                $("#queue-name-input").val(newName);
                $("#limit-input").val(newLimit);
                $("#start-at-input").val(newStartAt);
                $(".my-control-input-holder").hide();

                $(".save-changes").hide();
                $(".change-description").show();
            }
        })
});

//  кнопка сохраняющая сделанные изменения в информции о комнате - отправляет пост
//  запрос на сервер который меняет соответствующие поля
$(".save-room-changes").click(function() {

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

//  крест напротив имени пользователя - отправляет пост запрос на сервер который
//  удаляет связь между данными комнатой и пользователем
$(".delete-room-user").click(function() {

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

$(".delete-queue-user").click(function() {

    var clickedUser = $(this).closest(".my-participant-div");
    var userId = clickedUser.find(".user-id").text();
    var url = window.location.href;
    var queueId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Queue/RemoveUser", { id: queueId, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                clickedUser.remove();
            }
        })
});

//  крест напротив названия очереди - отправляет пост запрос на сервер который
//  удаляет запись с данной очередью
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

$(".delete-timetable").click(function() {

    var clickedTimetable = $(this).closest(".my-participant-div");
    var timetableId = clickedTimetable.find(".timetable-id").text();

    $.post("/Timetable/Delete", { id: timetableId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                clickedTimetable.remove();
            }
        })
});

//  значок шестеренки напротив названия очереди - открывает модальное окно и отправляет
//  пост запрос на сервер который возвращает список участников этой очереди
$(".queue-control").click(function() {

    var queueId = $(this).closest(".my-participant-div").find(".queue-id").text();
    $("#queue-id").text(queueId);
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Queue/GetUsers", { id: queueId, roomId: roomId })
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

//  кнопка на модальном окне управления очередью - отправляет пост запрос на сервер который
//  удаляет связь между данными очередью и пользователем и меняет значение позиции на -1 у 
//  всех оставшихся участников очереди
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

//  кнопка на панеле настройки аккаунта - либо скрывает параграфы с основной информацией
//  и делает инпуты видимыми либо отправляет пост запрос на сервер который обновляет
//  данные поля в записи текущего пользователя
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

//  кнопка на панеле настройки аккаунта - либо скрывает параграф с почтой и делает инпут
//  видимым либо отправляет пост запрос на сервер который обновляет адрес электронной 
//  почты у текущего пользователя и отправляет письмо с подтверждением
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

//  кнопка на панеле настройки аккаунта - либо делает видимыми инпуты для паролей
//  либо отправляет пост запрос на сервер который обновляет пароль и соль
//  текущего пользователя
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

//  значок поднятого или опущенного большого пальца напротив имени пользователя -
//  отправляет пост запрос на сервер который меняет статус модератора у данного
//  пользователя на противоположный
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

$(".candidates-input").change(function() {

    var searchedName = $("#user-name-cri").val();
    var searchedCompany = $("#user-company-cri").val();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);
    var target = $("#items-holder").empty();
    // target.empty();

    $.post("/Room/GetAvailableUsers", { id: roomId, searchedName: searchedName, searchedCompany: searchedCompany })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: OK");

                if (data != 0) {
                    data.forEach(element => {
                        var userDiv = $("<div></div>").addClass("separate-div room-for-join-div");
                        var hiddenP = $("<p></p>").addClass("hidden-p")
                            .attr("id", `queueId-${element.userId}`)
                            .text(element.userId)
                            .hide();
                        var rowDiv = $("<div></div>").addClass("row");
                        var nameDiv = $("<div></div>").addClass("col-6 content-div");
                        var nameP = $("<p></p>").text(element.name);
                        var companyDiv = $("<div></div>").addClass("col content-div");
                        var companyP = $("<p></p>").text(element.company);
                        var buttonDiv = $("<div></div>").addClass("col-2 join-btn-holder");
                        var button = $("<button></button>").addClass("btn btn-primary join-room-btn id-giver")
                            .attr("data-toggle", "modal")
                            .attr("data-target", "#sendRequesConfirmationModal")
                            .text("Invite");

                        target.append(
                            userDiv.append([
                                hiddenP, 
                                rowDiv.append([
                                    nameDiv.append(nameP),
                                    companyDiv.append(companyP),
                                    buttonDiv.append(button)
                                ])
                            ])
                        )
                    })
                }
            }
        })
})

$("#send-request-to-user").click(function() {

    var userId = $("#got-id").text();
    var comment = $("#comment-input").val();
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);

    $.post("/Room/AddRequest", { id: roomId, comment: comment, userId: userId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);
                $("#sendRequesConfirmationModal").modal("toggle");
                var toDeactivate = $(`#queueId-${userId}`).closest(".room-for-join-div").find(".join-room-btn");
                toDeactivate.prop("disabled", true);
            }
        })
});

$(".appointment-item").click(function() {

    var id = $(this).attr("id");

    if (id.slice(-1) == 0) {
        id = id.slice(0, -1) + "1";
        $(this).addClass("checked");
    } else {
        id = id.slice(0, -1) + "0";
        $(this).removeClass("checked");
    }

    $(this).attr("id", id);
});

$("#confirm-appointment-settings").click(function() {

    var url = window.location.href;
    var id = url.substring(url.lastIndexOf('/') + 1);

    var jsonObj = {
        Days: [],
        TimetableId: id
    };

    $(".my-day-holder").each(function(index) {

        var day = {
            WeekDay: $(this).closest(".row").find(".col-2").find("p").text(),
            Appointments: []
        };
        $(this).children().each(function() {

            if ($(this).hasClass("checked")) {
                day.Appointments.push(1);
            } else {
                day.Appointments.push(0);
            }
        })
        jsonObj.Days.push(day);
    })

    $.ajax({
        type: "POST",
        data: JSON.stringify(jsonObj),
        url: "/Timetable/AppointmentSettings",
        contentType: "application/json",
        success: function (result) { 
            if (result.redirectUrl !== undefined) {
                window.location.replace(result.redirectUrl);
            }
        }
    });
});

$(".cancel-appointment").click(function() {

    var timetableId = $(this).closest(".room-for-join-div").find(".hidden-p").text();
    var row = $(this).closest(".row");

    $.post("/Timetable/RemoveUser", { id: timetableId })
        .done(function(data) {

            if (data == null) {
                console.log("Status: FAIL");
            } else {
                console.log("Status: " + data);

                row.find(".cancel-appointment").hide();
                row.find(".make-appointment").show();
                row.find(".own-appointment").hide();
                row.find(".available-appoinments").show();
            }
        })
});

$(".activity-radio").click(function() {

    var status = $(this).is(":checked");
    var unwantedName = $(this).attr("id").slice(0, $(this).attr("id").indexOf("-"));
    var activities = $("#items-holder"),
        activitiesList = activities.children();

    activitiesList.each(function() {
        var activityName = $(this).find(".my-content-label-div p").text();
        if (activityName.toUpperCase() != unwantedName.toUpperCase() && status) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

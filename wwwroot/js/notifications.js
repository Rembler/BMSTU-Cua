"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

connection.on("ReceiveNotification", function (message) {
    var holder = $("#notifications-holder");
    var notification = $("<div></div>").addClass("content-div my-queue-member-holder");
    var text = $("<p></p>").text(message);
    holder.append(notification.append(text));
    alert("У вас новое уведомление!");
});

connection.on("ReceiveMessage", function (user, message) {
    var li = document.createElement("li");
    var div = document.createElement("div");
    div.classList.add("content-div", "my-msg-div");
    if (user == document.getElementById("current-user").textContent) {
        div.classList.add("my-own-msg-div");
    }
    var pSender = document.createElement("p");
    pSender.classList.add("my-msg-sender");
    var pBody = document.createElement("p");
    pBody.classList.add("my-msg-body");
    div.append(pSender, pBody);
    li.appendChild(div);
    document.getElementById("messagesList").appendChild(li);
    pBody.textContent = `${message}`;
    pSender.textContent = `${user}`;
});

connection.on("ReceiveRemovalWish", function (queueId, place) {
    console.log("Wish was gotten");
    if ($("#queue-id").text() == queueId) {
        var toRemove = $("#their-queue-members").children(".my-queue-member-holder").eq(place);
        toRemove.remove();
        if ($("#their-queue-members").children().length == 0) {
            var infoDiv = $("<div></div>").addClass("content-div my-queue-member-holder no-users-info");
            var infoP = $("<p></p>").text("Очередь пуста");
            $("#their-queue-members").append(infoDiv.append(infoP));
        }

        toRemove = $("#my-queue-members").children(".my-queue-member-holder").eq(place);
        toRemove.remove();
        if ($("#my-queue-members").children().length == 0) {
            var infoDiv = $("<div></div>").addClass("content-div my-queue-member-holder no-users-info");
            var infoP = $("<p></p>").text("Очередь пуста");
            $("#my-queue-members").append(infoDiv.append(infoP));
            $("#remove-first-user").prop("disabled", true);
        }
    }
});

connection.on("ReceiveAdditionWish", function (queueId, userName, userId) {
    console.log("Wish was gotten");
    if ($("#queue-id").text() == queueId) {

        var target = $("#my-queue-members");
        var subTarget = $("#their-queue-members");

        $(".no-users-info").remove();
        $("#remove-first-user").prop("disabled", false);
        var infoDiv = $("<div></div>").addClass("content-div my-queue-member-holder");
        var infoP = $("<p></p>").text(userName);
        var hiddenP = $("<p></p>").addClass("queue-user-id").text(userId).hide();

        target.append(infoDiv.append([infoP, hiddenP]));
        subTarget.append(infoDiv.append([infoP, hiddenP]).clone());
    }
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});
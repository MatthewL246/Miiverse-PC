window.wiiuMainApplication = {
    getScreenShot: function (screen) {
        console.log(`Getting screenshot: ${screen}`);

        var screenshot = null;
        if (screen === true) {
            screenshot =
                "iVBORw0KGgoAAAANSUhEUgAAAEAAAABAAgMAAADXB5lNAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAFiUAABYlAUlSJPAAAAAJUExURQCi6H9/f+0cJJKgY/4AAACsSURBVDjL7dO7DQMhEEXRiyUS5zThKiiBAPrZUhxaU+UGvOGzGzpx4EmQrsTRCAnaZdBZ0yXkcAmwhwppCwXiFjKELYAjiHgcQhDxfAtBxOsjBBFmQhBhJgQRZkIQYSYEJxzBCUdwwhGccIRBCGEQQhiEEAYhhEl0hEl0hEl0hEl0hIUwg8ZCmB0kCsw9IFJgrg6RvIdwD7crrbXqIY0n/IefCJovQvOw/v0xJyKGX/o6cyPrAAAAAElFTkSuQmCC";
        } else if (screen === false) {
            screenshot =
                "iVBORw0KGgoAAAANSUhEUgAAAEAAAABAAgMAAADXB5lNAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAFiUAABYlAUlSJPAAAAAJUExURQCi6H9/f+0cJJKgY/4AAADFSURBVDjL7dO7DcQgEIThkUNKcT9DQAlU4SbInSDBVHkBu+BHeMElt5k/aX9ZKwE+BiQpG4foUL4BAAD+8DuIkhwkEekOFfkO/Q2vlaQTc4IqotqCXQX0hbFEMOvw702d4CUSVAleIrsKwUtEIsEV2dRJcEWCKgmuyK4y7rF+3Q5kkU3dwCJBdZ6wzYQ/j5kYkHV4YkDS6YkBUc0TAyh5wiDrsIRB0mkJg6hmCQP6I52QZQmHJEs4RFnCgZ6YkPsDYnnAnA/9O0c8rY2eFAAAAABJRU5ErkJggg==";
        } else {
            console.error(`Unexpected value of screen: ${screen}`);
        }

        return screenshot;
    },
};

var basename = function (path)
{
	return path.split("/").pop();
};

Array.prototype.last = function ()
{
	return this[this.length - 1];
};

var Query = function (str)
{
	var strs = str.split("?");
	var qstr = strs.pop();
	this.path = strs.join("?");

	this.params = {};
	qstr.split("&").forEach(function (record)
			{
				var kv = record.split("=");
				this.params[kv[0]] = kv[1];
			}, this);
};	

var escapeHTML = function (str)
{
	return str.replace(/&/g, "&amp;")
		.replace(/</g, "&lt;")
		.replace(/>/g, "&gt;")
		.replace(/"/g, "&quot;")
		.replace(/'/g, "&#039;")
};

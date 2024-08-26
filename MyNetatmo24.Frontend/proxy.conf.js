module.exports = {
  "/api": {
    target:
      process.env["services__backend__https__0"] ||
      process.env["services__backend__http__0"],
    secure: process.env["NODE_ENV"] !== "development",
    pathRewrite: {
      "^/api": "",
    },
  },
};

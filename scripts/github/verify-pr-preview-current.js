module.exports = async function verifyPrPreviewCurrent({ github, context, core }) {
  const prNumber = Number(process.env.PREVIEW_PR_NUMBER);
  const expectedHeadSha = process.env.PREVIEW_EXPECTED_HEAD_SHA;

  if (!Number.isInteger(prNumber) || prNumber <= 0) {
    core.setFailed('PREVIEW_PR_NUMBER must be a positive integer.');
    return;
  }

  if (!expectedHeadSha) {
    core.setFailed('PREVIEW_EXPECTED_HEAD_SHA is required.');
    return;
  }

  const { owner, repo } = context.repo;
  const { data: pr } = await github.rest.pulls.get({
    owner,
    repo,
    pull_number: prNumber,
  });

  if (pr.state !== 'open' || !pr.base.ref.startsWith('release/')) {
    core.info(`PR #${pr.number} is ${pr.state} or no longer targets a release branch.`);
    core.setOutput('current', 'false');
    return;
  }

  if (pr.head.sha !== expectedHeadSha) {
    core.info(`PR #${pr.number} now points at ${pr.head.sha}, not ${expectedHeadSha}.`);
    core.setOutput('current', 'false');
    return;
  }

  core.setOutput('current', 'true');
};

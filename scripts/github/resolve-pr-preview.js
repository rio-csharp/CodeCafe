module.exports = async function resolvePrPreview({ github, context, core }) {
  const owner = context.repo.owner;
  const repo = context.repo.repo;
  const isDispatch = context.eventName === 'workflow_dispatch';
  const requiredWorkflowName = process.env.PREVIEW_REQUIRED_WORKFLOW_NAME || 'CI';
  let run = context.payload.workflow_run;
  let ciRunId = run?.id;

  if (isDispatch) {
    ciRunId = Number(process.env.PREVIEW_CI_RUN_ID);
    if (!Number.isInteger(ciRunId) || ciRunId <= 0) {
      core.setFailed('ci_run_id must be a positive integer.');
      return;
    }

    const response = await github.rest.actions.getWorkflowRun({
      owner,
      repo,
      run_id: ciRunId,
    });
    run = response.data;
  }

  if (!run) {
    core.setOutput('skip', 'true');
    return;
  }

  if (requiredWorkflowName && run.name !== requiredWorkflowName) {
    const message = `Workflow run ${ciRunId} is not a ${requiredWorkflowName} run. workflow=${run.name || ''}.`;
    if (isDispatch) {
      core.setFailed(message);
    } else {
      core.info(message);
      core.setOutput('skip', 'true');
    }
    return;
  }

  if (run.conclusion !== 'success' || !['push', 'pull_request'].includes(run.event)) {
    core.setOutput('skip', 'true');
    return;
  }

  let prNumber = isDispatch ? Number(process.env.PREVIEW_PR_NUMBER) : run.pull_requests?.[0]?.number;

  if (isDispatch && (!Number.isInteger(prNumber) || prNumber <= 0)) {
    core.setFailed('pr_number must be a positive integer.');
    return;
  }

  if (!prNumber && run?.head_sha) {
    const associated = await github.rest.repos.listPullRequestsAssociatedWithCommit({
      owner,
      repo,
      commit_sha: run.head_sha,
    });
    prNumber = associated.data.find(pr => pr.base.ref.startsWith('release/'))?.number;
  }

  if (!prNumber) {
    core.setOutput('skip', 'true');
    return;
  }

  const { data: pr } = await github.rest.pulls.get({
    owner,
    repo,
    pull_number: prNumber,
  });

  if (pr.state !== 'open' || !pr.base.ref.startsWith('release/')) {
    core.setOutput('skip', 'true');
    return;
  }

  if (run.head_sha !== pr.head.sha) {
    core.info(`Skipping preview deploy because CI run ${ciRunId} was built for ${run.head_sha}, but PR #${pr.number} now points at ${pr.head.sha}.`);
    core.setOutput('skip', 'true');
    return;
  }

  if (pr.head.repo.full_name !== `${owner}/${repo}`) {
    core.warning('Skipping preview deploy for forked PR to avoid exposing deployment secrets.');
    core.setOutput('skip', 'true');
    return;
  }

  core.setOutput('skip', 'false');
  core.setOutput('pr_number', String(pr.number));
  core.setOutput('head_sha', run.head_sha);
  core.setOutput('ci_run_id', String(ciRunId));
};

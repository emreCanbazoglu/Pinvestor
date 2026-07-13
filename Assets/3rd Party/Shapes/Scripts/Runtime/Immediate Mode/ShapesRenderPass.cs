using UnityEngine;
using UnityEngine.Rendering;
#if SHAPES_URP
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

#elif SHAPES_HDRP
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;

#endif

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes {

	#if SHAPES_URP
	internal class ShapesRenderPass : ScriptableRenderPass {
		DrawCommand drawCommand;

		class PassData {
			public DrawCommand drawCommand;
			public TextureHandle color;
			public TextureHandle depth;
		}

		public ShapesRenderPass Init( DrawCommand drawCommand ) {
			this.drawCommand = drawCommand;
			renderPassEvent = drawCommand.camEvt;
			return this;
		}

		public override void RecordRenderGraph( RenderGraph renderGraph, ContextContainer frameData ) {
			using( IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass( "Shapes Draw Command", out PassData passData ) ) {
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
				passData.drawCommand = drawCommand;
				passData.color = resourceData.activeColorTexture;
				passData.depth = resourceData.activeDepthTexture;
				builder.UseTexture( passData.color, AccessFlags.Write );
				builder.UseTexture( passData.depth, AccessFlags.Write );
				builder.AllowPassCulling( false );
				builder.SetRenderFunc( ( PassData data, UnsafeGraphContext ctx ) => {
					CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer( ctx.cmd );
					cmd.SetRenderTarget( data.color, data.depth );
					data.drawCommand.AppendToBuffer( cmd );
				} );
			}
		}

		public override void FrameCleanup( CommandBuffer cmd ) {
			DrawCommand.OnCommandRendered( drawCommand );
			drawCommand = null;
			ObjectPool<ShapesRenderPass>.Free( this );
		}
	}
	#elif SHAPES_HDRP
	public class ShapesRenderPass : CustomPass {
		// HDRP doesn't have ScriptableRenderPass stuff, so we use *one* custom pass per injection point, but branch inside of it instead
		// this does mean there will be redundancy/overhead in the way this is done, but, can't do much about it for now I think
		static List<DrawCommand> executingCommands = new List<DrawCommand>();
		protected override void Execute( ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult ) {

			if( DrawCommand.cBuffersRendering.TryGetValue( hdCamera.camera, out List<DrawCommand> cmds ) ) {
				for( int i = 0; i < cmds.Count; i++ ) {
					if( cmds[i].camEvt == injectionPoint ) {
						executingCommands.Add( cmds[i] );
						cmds[i].AppendToBuffer( cmd );
					}
				}
			}

			// if we added commands, execute them immediately
			if( executingCommands.Count > 0 ) {
				context.ExecuteCommandBuffer( cmd ); // we have to execute it because OnCommandRendered might want to destroy used materials
				cmd.Clear();
				foreach( DrawCommand drawCommand in executingCommands )
					DrawCommand.OnCommandRendered( drawCommand ); // deletes cached assets
			}
			executingCommands.Clear();
		}
	}
	#endif

}